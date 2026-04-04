using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ElvantoSync.ElvantoService;
using ElvantoSync.Extensions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings;
using Fegmm.ChurchTools;
using Fegmm.Elvanto.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using MimeMapping;


namespace ElvantoSync.ChurchTools;

using ElvantoSongTuple = (Song song, List<(Arrangement arrangement, List<ArrangementKey> keys)> arrangements);
using CtSong = Fegmm.ChurchTools.Songs.SongsGetResponse_data;

public class SongsToChurchToolsSync(
    IElvantoClient elvanto,
    ChurchToolsClient churchTools,
    DbContext dbContext,
    IOptions<SongsToChurchToolsSyncSettings> settings,
    ILogger<SongsToChurchToolsSync> logger
) : Sync<ElvantoSongTuple, CtSong>(dbContext, settings, logger)
{
    private string csrfToken;

    public override string FallbackFromKeySelector(ElvantoSongTuple eSong) => eSong.song.Title + "_" + eSong.song.Artist;
    public override string FallbackToKeySelector(CtSong i) => i.Name + "_" + i.Author;
    
    public override string FromKeySelector(ElvantoSongTuple i) => i.song.Id;
    public override string ToKeySelector(CtSong i) => i.Id.ToString();

    public override async Task<IEnumerable<ElvantoSongTuple>> GetFromAsync()
    {
        var songs = (await elvanto.GetSongsAsync(new() { Files = true }))
            .Where(i => i.Status == 1); // Only include active songs

        return await songs
            .Chunk(10)
            .Select(async chunk => await chunk
                .Select(async song => (
                    song,
                    arrangements: await (await elvanto.GetArrangementsAsync(new()
                    {
                        SongId = song.Id,
                        Files = true
                    })).Select(async i => (arrangement: i, keys: await elvanto.GetArrangementKeysAsync(new()
                    {
                        ArrangementId = i.Id,
                        Files = true
                    })))
                    .ToAsyncEnumerable()
                    .ToListAsync()
                ))
                .WhenAll()
            )
            .ToAsyncEnumerable()
            .SelectMany(i => i)
            .ToListAsync();
    }

    public override async Task<IEnumerable<CtSong>> GetToAsync()
    {
        List<CtSong> allSongs = new();

        int pages = 1;
        for (int i = 1; i <= pages; i++)
        {
            var listResponse = await churchTools.Songs.GetAsSongsGetResponseAsync(conf =>
            {
                conf.QueryParameters.Limit = 200;
                conf.QueryParameters.Page = i;
                conf.QueryParameters.IncludeAsGetIncludeQueryParameterType = [Fegmm.ChurchTools.Songs.GetIncludeQueryParameterType.Arrangements, Fegmm.ChurchTools.Songs.GetIncludeQueryParameterType.Tags];
            });
            pages = listResponse.Meta.Pagination.LastPage.Value;
            allSongs.AddRange(listResponse.Data);
        }

        csrfToken = (await churchTools.Csrftoken.GetAsCsrftokenGetResponseAsync()).Data;

        return allSongs;
    }

    #region Songs
    protected override async Task<string> AddMissing(ElvantoSongTuple missing)
    {
        var response = await churchTools.Songs.PostAsSongsPostResponseAsync(new()
        {
            Author = missing.song.Artist,
            ShouldPractice = missing.song.Learn == 1,
            Ccli = missing.song.Number,
            Name = missing.song.Title,
            Copyright = string.Join(", ", missing.arrangements.Select(a => a.arrangement.Copyright).Where(c => c is not null).Distinct()),
            CategoryId = missing.song.Categories.Category
                .Select(i => settings.Value.CategoryMap.GetValueOrDefault(i.Id))
                .FirstOrDefault(i => i is not null) ?? settings.Value.DefaultCategoryId,
        });
        int churchToolsId = response.Data.Id!.Value;

        CtSong newlyCreateSong = await response.Data.ConvertTo<CtSong>();
        var newSongArrangements = await churchTools.Songs[churchToolsId].Arrangements.GetAsArrangementsGetResponseAsync();
        newlyCreateSong.Arrangements = (await newSongArrangements.Data.Select(i => i.ConvertTo<Fegmm.ChurchTools.Songs.SongsGetResponse_data_arrangements>()).WhenAll()).ToList();
        newlyCreateSong.AdditionalData["tags"] = new UntypedArray([]);

        await HandleTags(missing, newlyCreateSong);
        await HandleArragements(missing, newlyCreateSong);


        return churchToolsId.ToString();
    }

    protected override async Task UpdateMatch(ElvantoSongTuple from, CtSong to)
    {
        if (to.Meta.ModifiedDate < from.song.DateModified)
        {
            await churchTools.Songs[to.Id.Value].PutAsWithSongPutResponseAsync(new()
            {
                Author = from.song.Artist,
                ShouldPractice = from.song.Learn == 1,
                Ccli = from.song.Number,
                Name = from.song.Title,
                CategoryId = from.song.Categories.Category
                    .Select(i => settings.Value.CategoryMap.GetValueOrDefault(i.Id))
                    .FirstOrDefault(i => i is not null) ?? settings.Value.DefaultCategoryId,

                Copyright = to.Copyright
            });
        }
        else if (to.Meta.ModifiedDate < from.arrangements.First().arrangement.DateModified)
        {
            await churchTools.Songs[to.Id.Value].PutAsWithSongPutResponseAsync(new()
            {
                Author = to.Author,
                ShouldPractice = to.ShouldPractice,
                Ccli = to.Ccli,
                Name = to.Name,
                CategoryId = to.Category.Id,
                Copyright = to.Copyright
            });
        }

        await HandleTags(from, to);
        await HandleArragements(from, to);
    }

    protected override async Task RemoveAdditional(CtSong additional)
    {
        await churchTools.Songs[additional.Id.Value].DeleteAsync();
    }
    #endregion

    #region Arrangements
    private async Task HandleArragements(ElvantoSongTuple eSong, CtSong ctSong)
    {
        // Patch elvanto and church tools default arrangement to have the same name, so they are matched together
        var eDefaultArrangement = eSong.arrangements.FirstOrDefault(i => i.arrangement.Name == "Standard Arrangement");
        eDefaultArrangement.arrangement?.Name = "Standard-Arrangement";

        // If one arrangement has multiple keys, we duplicate the arrangement in church tools to be able to assign the different keys to each arrangement. 
        // We match arrangements by name, so we need to make sure that the arrangement names are unique. We add the keys to the arrangement name in Elvanto, so we can match them together.
        // If no or single key is set, we don't add the keys to the arrangement name, so we don't create unnecessary arrangements in church tools.
        // If name is not empty or "Elvanto" or the only one we append the key name to the arrangement name
        var eArrangements = eSong.arrangements.SelectMany(i =>
        {
            if (i.keys.Count == 0)
            {
                return [i.arrangement];
            }

            return i.keys.Select(k =>
            {
                var key = k.KeyStarting ?? i.arrangement.ChordChartKey ?? i.arrangement.KeyMale ?? i.arrangement.KeyFemale;
                var arrangementName = i.arrangement.Name;
                if (!string.IsNullOrWhiteSpace(k.Name) && k.Name != "Elvanto")
                {
                    arrangementName += $" {k.Name}";
                }
                if (i.keys.Count != 1)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        arrangementName += $" ({key})";
                    }
                }

                return new Arrangement
                {
                    Name = arrangementName,
                    ChordChartKey = key,
                    Files = new Files()
                    {
                        File = [
                            .. eSong.song.Files?.File ?? [],
                            .. i.arrangement.Files?.File ?? [],
                            .. k.Files?.File ?? []]
                    },
                    Bpm = i.arrangement.Bpm,
                    Minutes = i.arrangement.Minutes,
                    Seconds = i.arrangement.Seconds,
                    KeyMale = i.arrangement.KeyMale,
                    KeyFemale = i.arrangement.KeyFemale,
                    DateModified = i.arrangement.DateModified,
                };
            });
        }).ToList();

        var arrangementComparison = eArrangements.CompareTo(ctSong.Arrangements, i => i.Name, i => i.Name);
        foreach (var additional in arrangementComparison.additional)
        {
            try
            {
                await AddArrangement(eSong, ctSong, additional);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding arrangement {ArrangementName} to song with id {SongId}", additional.Name, ctSong.Id.Value);
            }
        }

        foreach (var match in arrangementComparison.matches)
        {
            try
            {
                await UpdateArrangement(eSong, ctSong, match.Item1, match.Item2);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating arrangement {ArrangementName} in song with id {SongId}", match.Item1.Name, ctSong.Id.Value);
            }
        }

        if (settings.Value.DeleteAdditionalArrangements)
        {
            foreach (var missing in arrangementComparison.missing)
            {
                try
                {
                    await RemoveArrangement(eSong, ctSong, missing);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error removing arrangement {ArrangementName} from song with id {SongId}", missing.Name, ctSong.Id.Value);
                }
            }
        }
    }

    private async Task AddArrangement(ElvantoSongTuple eSong, CtSong ctSong, Arrangement eArrangement)
    {
        var hasMinutes = int.TryParse(eArrangement.Minutes, out int minutes);
        var hasSeconds = int.TryParse(eArrangement.Seconds, out int seconds);

        var ctArrangement = await churchTools.Songs[ctSong.Id.Value].Arrangements.PostAsArrangementsPostResponseAsync(new()
        {
            Name = eArrangement.Name,
            Duration = !hasMinutes && !hasSeconds ? null : ((hasMinutes ? minutes * 60 : 0) + (hasSeconds ? seconds : 0)),
            Tempo = int.TryParse(eArrangement.Bpm, out int bpm) && bpm > 0 ? bpm : null,
            Key = new()
            {
                String = eArrangement.ChordChartKey ?? eArrangement.KeyMale ?? eArrangement.KeyFemale,
            },
        });
        var convertedArrangement = await ctArrangement.Data.ConvertTo<Fegmm.ChurchTools.Songs.SongsGetResponse_data_arrangements>();

        await HandleFiles(eArrangement, convertedArrangement);
    }

    private async Task UpdateArrangement(ElvantoSongTuple eSong, CtSong ctSong, Arrangement eArrangement, Fegmm.ChurchTools.Songs.SongsGetResponse_data_arrangements ctArrangement)
    {
        if (eArrangement.DateModified < ctArrangement.Meta.ModifiedDate)
        {
            return;
        }

        var hasMinutes = int.TryParse(eArrangement.Minutes, out int minutes);
        var hasSeconds = int.TryParse(eArrangement.Seconds, out int seconds);

        await churchTools.Songs[ctSong.Id.Value].Arrangements[ctArrangement.Id.Value].PutAsWithArrangementPutResponseAsync(new()
        {
            Tempo = int.TryParse(eArrangement.Bpm, out int bpm) && bpm > 0 ? bpm : null,
            Name = eArrangement.Name,
            Duration = !hasMinutes && !hasSeconds ? null : ((hasMinutes ? minutes * 60 : 0) + (hasSeconds ? seconds : 0)),
            Beat = ctArrangement.Beat,
            Key = new()
            {
                String = eArrangement.ChordChartKey ?? eArrangement.KeyMale ?? eArrangement.KeyFemale
            },
            Description = ctArrangement.Description,
            SourceId = ctArrangement.SourceId,
            SourceReference = ctArrangement.SourceReference,
        });

        await HandleFiles(eArrangement, ctArrangement);
    }

    private async Task RemoveArrangement(ElvantoSongTuple eSong, CtSong ctSong, Fegmm.ChurchTools.Songs.SongsGetResponse_data_arrangements missing)
    {
        await churchTools.Songs[ctSong.Id.Value].Arrangements[missing.Id.Value].DeleteAsync();
    }
    #endregion

    #region Files
    private async Task HandleFiles(Arrangement eArrangement, Fegmm.ChurchTools.Songs.SongsGetResponse_data_arrangements ctArrangement)
    {
        var eFiles = eArrangement.Files?.File ?? [];
        var ctFiles = (await churchTools.Files["song_arrangement"][ctArrangement.Id.Value.ToString()].GetAsWithDomainIdentifierGetResponseAsync()).Data;
        var fileComparison = ctFiles.CompareTo(eFiles, i => i.Name, i => i.Title);

        foreach (var additional in fileComparison.missing)
        {
            try
            {
                await AddFile(ctArrangement.Id.Value, additional);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding file {FileName} to arrangement with id {ArrangementId}", additional.Title, ctArrangement.Id.Value);
            }
        }

        foreach (var match in fileComparison.matches)
        {
            try
            {
                await UpdateFile(ctArrangement.Id.Value, match.Item1, match.Item2);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating file {FileName} in arrangement with id {ArrangementId}", match.Item2.Title, ctArrangement.Id.Value);
            }
        }

        if (settings.Value.DeleteAdditionalFiles)
        {
            foreach (var missing in fileComparison.additional)
            {
                try
                {
                    await RemoveFile(missing);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error removing file with id {FileId} from arrangement with id {ArrangementId}", missing.Id, ctArrangement.Id.Value);
                }
            }
        }
    }

    private async Task AddFile(int arrangementId, FileObject fileToAdd)
    {
        var content = new MultipartBody();
        if (IsLinkFile(fileToAdd))
        {
            await churchTools.Files["song_arrangement"][arrangementId.ToString()].Link.PostAsLinkPostResponseAsync(new()
            {
                Name = fileToAdd.Title,
                Url = fileToAdd.Content,
            });
            return;
        }

        if (fileToAdd.Html == 1)
        {
            var htmlContent = $"""
                <html>
                    <body>
                        {fileToAdd.Content}
                    </body>
                </html>
            """;
            content.AddOrReplacePart("files[]", KnownMimeTypes.Html, Encoding.UTF8.GetBytes(htmlContent), $"{fileToAdd.Title}.html");
        }
        else
        {
            var url = fileToAdd.Content;
            var extension = Path.GetExtension(url).ToLower();
            using var fileResponse = await new HttpClient().GetAsync(url);
            var fileStream = await fileResponse.Content.ReadAsStreamAsync();

            content.AddOrReplacePart("files[]", fileResponse.Content.Headers.ContentType.MediaType, fileStream, $"{fileToAdd.Title}{extension}");
        }
        await churchTools.Files["song_arrangement"][arrangementId.ToString()]
            .PostAsWithDomainIdentifierPostResponseAsync(content, conf => conf.Headers.Add("Csrf-Token", csrfToken));
    }

    private static bool IsLinkFile(FileObject fileToAdd)
    {
        return fileToAdd.Content.StartsWith("http") && !fileToAdd.Content.Contains(".cloudfront.net");
    }

    private async Task UpdateFile(int arrangementId, Fegmm.ChurchTools.Files.Item.Item.WithDomainIdentifierGetResponse_data ctFile, FileObject eFile)
    {
        DateTimeOffset? GetElvantoFileDate(FileObject file)
        {
            var fileNameParts = file.Content.Split('_', '.');
            if (fileNameParts.Length < 2)
            {
                return null;
            }

            var timestampPart = fileNameParts[^2];
            if (!long.TryParse(timestampPart, out long timestamp))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(timestamp);
        }

        if (IsLinkFile(eFile))
        {
            if (eFile.Content == ctFile.FileUrl)
            {
                return;
            }
        }
        else if (GetElvantoFileDate(eFile) is not DateTimeOffset modifiedDate || modifiedDate <= ctFile.Meta.ModifiedDate)
        {
            return;
        }

        await RemoveFile(ctFile);
        await AddFile(arrangementId, eFile);
    }

    private async Task RemoveFile(Fegmm.ChurchTools.Files.Item.Item.WithDomainIdentifierGetResponse_data ctFile) => await churchTools.Files[ctFile.Id.ToString()].DeleteAsync();

    #endregion

    #region Tags
    private async Task HandleTags((Song song, IEnumerable<(Arrangement arrangement, List<ArrangementKey> keys)> arrangements) eSong, CtSong ctSong)
    {
        var eTags = eSong.song.Categories?.Category?.Where(i => !settings.Value.CategoryMap.ContainsKey(i.Id)) ?? [];
        var ctTags = await ((ctSong.AdditionalData["tags"] as UntypedArray)?.ConvertTo<Fegmm.ChurchTools.Tags.Item.Item.WithDomainGetResponse_data>() ?? throw new InvalidOperationException("Tags not included in additional data"));

        var tagComparison = ctTags.CompareTo(eTags, i => i.Name, i => i.Name);

        foreach (var additional in tagComparison.missing)
        {
            try
            {
                await AddTag(ctSong.Id.Value, additional);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding tag {TagName} to song with id {SongId}", additional.Name, ctSong.Id.Value);
            }
        }

        if (settings.Value.DeleteAdditionalTags)
        {
            foreach (var missing in tagComparison.additional)
            {
                try
                {
                    await RemoveTags(ctSong.Id.Value, missing.Id.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error removing tag with id {TagId}", missing.Id.Value);
                }
            }
        }
    }

    private async Task AddTag(int churchToolsId, IdName tagToAdd)
        => await churchTools.Tags["song"][churchToolsId].PostAsWithDomainPostResponseAsync(new()
        {
            Name = tagToAdd.Name,
        });

    private async Task RemoveTags(int songId, int tagId)
        => await churchTools.Tags["song"][songId.ToString()][tagId.ToString()].DeleteAsync();
    #endregion
}