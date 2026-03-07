using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.ChurchTools;
using Fegmm.ChurchTools;
using Fegmm.ChurchTools.Persons;
using Fegmm.ChurchTools.Persons.Item;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions;
using ChurchToolPerson = Fegmm.ChurchTools.Persons.PersonsGetResponse_data;

namespace ElvantoSync.ChurchTools;

internal class PeopleToChurchToolsSync(IElvantoClient elvanto,
    ChurchToolsClient churchTools,
    DbContext dbContext,
    IOptions<PeopleToChurchToolsSyncSettings> settings,
    ILogger<PeopleToChurchToolsSync> logger
) : Sync<Person, ChurchToolPerson>(dbContext, settings, logger)
{
    private string csrfToken;
    public override string FromKeySelector(Person i) => i.Id;
    public override string ToKeySelector(ChurchToolPerson i) => i.Id.ToString();

    public override string FallbackFromKeySelector(Person i)
    {
        ParseFirstName(i, out var firstName, out var _);
        return $"{firstName}-{i.Lastname}-{i.Email}".Trim();
    }

    public override string FallbackToKeySelector(ChurchToolPerson i) => $"{i.FirstName}-{i.LastName}-{i.Email}".Trim();

    public override async Task<IEnumerable<Person>> GetFromAsync()
        => (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest()
        {
            Category_id = settings.Value.CategoryToSync,
            Fields = ["birthday", "marital_status", "gender", "giving_number", "home_address", "home_address2", "home_city", "home_country", "home_postcode"]
        }))
            .People.Person
            .Where(p => !settings.Value.ExceptFromSync.Contains(p.Id)); // Special exceptions

    public override async Task<IEnumerable<ChurchToolPerson>> GetToAsync()
    {
        List<ChurchToolPerson> allPersons = new();

        int pages = 1;
        for (int i = 1; i <= pages; i++)
        {
            var listResponse = await churchTools.Persons.GetAsPersonsGetResponseAsync(conf =>
            {
                conf.QueryParameters.Limit = 200;
                conf.QueryParameters.Page = i;
            });
            pages = listResponse.Meta.Pagination.LastPage.Value;
            allPersons.AddRange(listResponse.Data);
        }

        csrfToken = (await churchTools.Csrftoken.GetAsCsrftokenGetResponseAsync()).Data;

        return allPersons;
    }

    protected override async Task<string> AddMissing(Person missing)
    {
        string firstName, nickName;
        ParseFirstName(missing, out firstName, out nickName);

        var requestBody = new PersonsPostRequestBody()
        {
            // TODO: Image
            // TODO: Title
            FirstName = firstName,
            Nickname = nickName,
            LastName = missing.Lastname,
            PhonePrivate = missing.Phone,
            Mobile = missing.Mobile,
            // TODO: Additional phones
            Email = missing.Email,
            Street = missing.Home_address,
            AddressAddition = missing.Home_address2,
            Zip = missing.Home_postcode,
            City = missing.Home_city,
            Country = missing.Home_country,
            Birthday = DateOnly.Parse(missing.Birthday),
            // TODO: Job
            // TODO: Gemeindeaufnahmedatum
            // TODO: Taufdatum
            FamilyStatusId = missing.Marital_status switch
            {
                "Single" => 1,
                "Married" => 2,
                "Separated" => 3,
                "Divorced" => 4,
                "Widowed" => 5,
                "Engaged" => 6,
                _ => 0
            },
            SexId = missing.Gender switch
            {
                "Male" => 1,
                "Female" => 2,
                _ => 0
            },
            // TODO: Schlüsselbesitz
            // TODO: Zugehörigkeit Gottesdienst
            // TODO: In diesem Bereich könnte ich mir vorstellen mizuarbeiten
            // TODO: Bemerkung zur Mitarbeit
            // TODO: Verhaltenskodex
            // TODO: Führungszeugnis
            // TODO: Selbstverpflichtung
            // TODO: Metro-Karte
            // TODO: Datenschutzverordnung - Datum der Genehmigung
            // TODO: Datenschutzverordnung - Genehmigungen
            // TODO: Verpflichtung zur Wahrung der Vertraulichkeit
            OptigemId = missing.Giving_number,
            // TODO: Datum der Archivierung
            // TODO: Sterbedatum
            // TODO: Gottesdienst-Kategorien 
            StatusId = 3,
            DepartmentIds = [1],
            CampusId = 0,
            PrivacyPolicyAgreement = new()
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                TypeId = 3,
                WhoId = 1
            },
        };
        PersonsPostResponse response = await churchTools.Persons.PostAsPersonsPostResponseAsync(requestBody);

        var churchToolsId = response.Data.Id!.Value;

        try
        {
            await SetAvatar(churchToolsId, missing.Picture);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set avatar for person with ID {PersonId}", churchToolsId);
        }

        try
        {
            await churchTools.Notes["person"][churchToolsId].PostAsWithDomainPostResponseAsync(new()
            {
                Text = GetSyncNoteText(missing),
                CommentViewerId = 2, // Admin only
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create sync note for person with ID {PersonId}", churchToolsId);
        }

        return churchToolsId.ToString();
    }

    protected override async Task UpdateMatch(Person from, ChurchToolPerson to)
    {
        var churchToolsId = to.Id.Value;
        var syncNote = await GetSyncNote(churchToolsId);
        string firstName, nickName;
        ParseFirstName(from, out firstName, out nickName);


        WithPersonPatchResponse response = await churchTools.Persons[churchToolsId].PatchAsWithPersonPatchResponseAsync(new()
        {
            // TODO: Image
            // TODO: Title
            FirstName = firstName,
            Nickname = nickName,
            LastName = from.Lastname,
            PhonePrivate = from.Phone,
            Mobile = from.Mobile,
            // TODO: Additional phones
            Email = from.Email,
            Street = from.Home_address,
            AddressAddition = from.Home_address2,
            Zip = from.Home_postcode,
            City = from.Home_city,
            Country = from.Home_country,
            Birthday = DateOnly.Parse(from.Birthday),
            // TODO: Job
            // TODO: Gemeindeaufnahmedatum
            // TODO: Taufdatum
            FamilyStatusId = from.Marital_status switch
            {
                "Single" => 1,
                "Married" => 2,
                "Separated" => 3,
                "Divorced" => 4,
                "Widowed" => 5,
                "Engaged" => 6,
                _ => 0
            },
            SexId = from.Gender switch
            {
                "Male" => 1,
                "Female" => 2,
                _ => 0
            },
            // TODO: Schlüsselbesitz
            // TODO: Zugehörigkeit Gottesdienst
            // TODO: In diesem Bereich könnte ich mir vorstellen mizuarbeiten
            // TODO: Bemerkung zur Mitarbeit
            // TODO: Verhaltenskodex
            // TODO: Führungszeugnis
            // TODO: Selbstverpflichtung
            // TODO: Metro-Karte
            // TODO: Datenschutzverordnung - Datum der Genehmigung
            // TODO: Datenschutzverordnung - Genehmigungen
            // TODO: Verpflichtung zur Wahrung der Vertraulichkeit
            OptigemId = from.Giving_number,
            // TODO: Datum der Archivierung
            // TODO: Sterbedatum
            // TODO: Gottesdienst-Kategorien 
            StatusId = 3,
            DepartmentIds = [1],
            CampusId = 0,
            PrivacyPolicyAgreement = new()
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                TypeId = 3,
                WhoId = 1
            },
        });

        try
        {
            await SetAvatar(churchToolsId, from.Picture);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set avatar for person with ID {PersonId}", churchToolsId);
        }

        try
        {
            if (syncNote != null)
            {
                await churchTools.Notes["person"][churchToolsId][syncNote.Id.Value].PutAsWithNotePutResponseAsync(new()
                {
                    Text = GetSyncNoteText(from),
                    CommentViewerId = 2, // Admin only
                });
            }
            else
            {
                await churchTools.Notes["person"][churchToolsId].PostAsWithDomainPostResponseAsync(new()
                {
                    Text = GetSyncNoteText(from),
                    CommentViewerId = 2, // Admin only
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update sync note for person with ID {PersonId}", churchToolsId);
        }

    }

    private async Task<Fegmm.ChurchTools.Notes.Item.Item.WithDomainGetResponse_data> GetSyncNote(int churchToolsId)
    {
        return (await churchTools.Notes["person"][churchToolsId].GetAsWithDomainGetResponseAsync())
            .Data
            .FirstOrDefault(n => n.Text.StartsWith($"Elvanto Sync Note:"));
    }

    protected override async Task RemoveAdditional(ChurchToolPerson additional)
    {
        var syncNote = await GetSyncNote(additional.Id.Value);
        if (syncNote == null)
        {
            // Only archive if synced using this tool, otherwise we might delete manually created entries
            return;
        }

        await churchTools.Persons[additional.Id.Value].Archive.PostAsync(new() { Archived = true });
    }

    private static string GetSyncNoteText(Person from)
        => $@"Elvanto Sync Note:
                - Elvanto ID: {from.Id}
                - Last Updated: {DateTime.UtcNow}";

    private static void ParseFirstName(Person missing, out string firstName, out string nickName)
    {
        var regex = new Regex(@"^(?<firstName>[^\(]+)(\((?<nickName>[^\)]+)\))?$");
        var match = regex.Match(missing.Firstname);
        firstName = match.Success ? match.Groups["firstName"].Value : missing.Firstname;
        nickName = match.Success ? match.Groups["nickName"].Value : null;
    }

    private async Task SetAvatar(int personId, string imageUrl)
    {
        var extension = Path.GetExtension(imageUrl).ToLower();
        var content = new MultipartBody();
        using var imageResponse = await new HttpClient().GetAsync(imageUrl);
        var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
        content.AddOrReplacePart("files[]", imageResponse.Content.Headers.ContentType.MediaType, imageBytes, $"{personId}.${extension}");
        var response = await churchTools.Files["avatar"][personId.ToString()]
            .PostAsWithDomainIdentifierPostResponseAsync(content, conf => conf.Headers.Add("Csrf-Token", csrfToken));
    }
}

