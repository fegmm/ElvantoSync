using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ElvantoSync.ElvantoService;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.ChurchTools;
using Fegmm.ChurchTools;
using Fegmm.ChurchTools.Persons;
using Fegmm.ChurchTools.Persons.Item;
using Fegmm.Elvanto;
using Fegmm.Elvanto.Models;
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
    private ElvantoCustomFields fields => settings.Value.ElvantoCustomFields;
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
        => (await elvanto.PeopleGetAllAsync(new()
        {
            Archived = FilterEnum.No,
            Fields = [.. new[] {
                PersonAdditionalFields.Marital_status,
                PersonAdditionalFields.Birthday,
                PersonAdditionalFields.Gender,
                PersonAdditionalFields.Giving_number,
                PersonAdditionalFields.Home_address,
                PersonAdditionalFields.Home_address2,
                PersonAdditionalFields.Home_city,
                PersonAdditionalFields.Home_country,
                PersonAdditionalFields.Home_postcode,
                settings.Value.ElvantoCustomFields.Title,
                settings.Value.ElvantoCustomFields.AdditionalPhoneNumbers,
                settings.Value.ElvantoCustomFields.Job,
                settings.Value.ElvantoCustomFields.DateOfEntry,
                settings.Value.ElvantoCustomFields.DateOfBaptism,
                settings.Value.ElvantoCustomFields.Keyholder,
                settings.Value.ElvantoCustomFields.InterestToVolunteerIn,
                settings.Value.ElvantoCustomFields.NoteOnVolunteering,
                settings.Value.ElvantoCustomFields.CodeOfConduct,
                settings.Value.ElvantoCustomFields.CertificateOfConduct,
                settings.Value.ElvantoCustomFields.SelfCommitment,
                settings.Value.ElvantoCustomFields.MetroCard,
                settings.Value.ElvantoCustomFields.DateOfApprovalOfPrivacyPolicy,
                settings.Value.ElvantoCustomFields.ApprovalOfPrivacyPolicy,
                settings.Value.ElvantoCustomFields.DateOfNonDisclosureAgreement,
                settings.Value.ElvantoCustomFields.DateOfArchiving,
                settings.Value.ElvantoCustomFields.DateOfDeath,
            }.Where(i => i != null)]
        }))
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
                conf.QueryParameters.IsArchived = false;
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
            Title = missing.GetStringCustomField(fields.Title),
            FirstName = firstName,
            Nickname = nickName,
            LastName = missing.Lastname,
            PhonePrivate = missing.Phone,
            Mobile = missing.Mobile,
            Email = missing.Email,
            Street = missing.HomeAddress,
            AddressAddition = missing.HomeAddress2,
            Zip = missing.HomePostcode,
            City = missing.HomeCity,
            Country = missing.HomeCountry,
            Birthday = missing.Birthday,
            Job = missing.GetStringCustomField(fields.Job),
            DateOfEntry = missing.GetDateCustomField(fields.DateOfEntry)?.ToDateTime(TimeOnly.MinValue),
            DateOfBaptism = missing.GetDateCustomField(fields.DateOfBaptism),
            FamilyStatusId = missing.MaritalStatus switch
            {
                MaritalStatus.Single => 1,
                MaritalStatus.Married => 2,
                MaritalStatus.Separated => 3,
                MaritalStatus.Divorced => 4,
                MaritalStatus.Widowed => 5,
                MaritalStatus.Engaged => 6,
                _ => 0
            },
            SexId = missing.Gender switch
            {
                Gender.Male => 1,
                Gender.Female => 2,
                _ => 0
            },
            OptigemId = missing.GivingNumber,
            DateOfDeath = missing.GetDateCustomField(fields.DateOfDeath),
            StatusId = settings.Value.Status.GetValueOrDefault(missing.CategoryId, settings.Value.DefaultStatusId),
            DepartmentIds = [settings.Value.Departments.GetValueOrDefault(missing.CategoryId, settings.Value.DefaultDepartment)],
            CampusId = 0,
            PrivacyPolicyAgreement = new()
            {
                Date = missing.GetDateCustomField(fields.ApprovalOfPrivacyPolicy) ?? DateOnly.FromDateTime(DateTime.UtcNow),
                TypeId = 3,
                WhoId = 1
            },
            AdditionalData = await SetCustomFields(missing)
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

    private async Task<Dictionary<string, object>> SetCustomFields(Person missing)
    {
        var churchFields = settings.Value.ChurchToolsCustomFields;
        try
        {
            return new Dictionary<string, object>()
            {
                [churchFields.AdditionalPhoneNumbers] = missing.GetStringCustomField(fields.AdditionalPhoneNumbers),
                [churchFields.ApprovalOfPrivacyPolicy] = (await missing.GetMultiOptionCustomField(fields.ApprovalOfPrivacyPolicy))?
                            .CustomField?
                            .Select(i => settings.Value.PrivacyApprovals[i.Id]),
                [churchFields.CertificateOfConduct] = missing.GetDateCustomField(fields.CertificateOfConduct),
                [churchFields.CodeOfConduct] = (await missing.GetSingleOptionCustomField(fields.CodeOfConduct))?.Id == settings.Value.HasCodeOfConductId,
                [churchFields.DateOfArchiving] = missing.GetDateCustomField(fields.DateOfArchiving),
                [churchFields.DateOfNonDisclosureAgreement] = missing.GetDateCustomField(fields.DateOfNonDisclosureAgreement),
                [churchFields.InterestToVolunteerIn] = (await missing.GetMultiOptionCustomField(fields.InterestToVolunteerIn))?
                            .CustomField?
                            .Where(i => i.Id is not null) // Elvanto sometimes returns "" as option 🤦‍♂️
                            .Select(i => settings.Value.InterestToVolunteerInOptions[i.Id]),
                [churchFields.Keyholder] = missing.GetStringCustomField(fields.Keyholder),
                [churchFields.MetroCard] = (await missing.GetSingleOptionCustomField(fields.MetroCard))?.Id == settings.Value.HasMetroCardId,
                [churchFields.NoteOnVolunteering] = missing.GetStringCustomField(fields.NoteOnVolunteering),
                [churchFields.SelfCommitment] = (await missing.GetSingleOptionCustomField(fields.SelfCommitment))?.Id == settings.Value.HasSelfCommitmentId,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve custom fields from ChurchTools. Please check the configuration and ensure that the custom field IDs are correct.");
            throw;
        }
    }

    protected override async Task UpdateMatch(Person from, ChurchToolPerson to)
    {
        if (from.DateModified <= to.Meta.ModifiedDate)
        {
            logger.LogInformation("Skipping update for person with ID {PersonId} because source is not newer than target", to.Id);
            return;
        }

        var churchToolsId = to.Id.Value;
        var syncNote = await GetSyncNote(churchToolsId);
        string firstName, nickName;
        ParseFirstName(from, out firstName, out nickName);

        WithPersonPatchResponse response = await churchTools.Persons[churchToolsId].PatchAsWithPersonPatchResponseAsync(new()
        {
            Title = from.GetStringCustomField(fields.Title),
            FirstName = firstName,
            Nickname = nickName,
            LastName = from.Lastname,
            PhonePrivate = from.Phone,
            Mobile = from.Mobile,
            Email = from.Email,
            Street = from.HomeAddress,
            AddressAddition = from.HomeAddress2,
            Zip = from.HomePostcode,
            City = from.HomeCity,
            Country = from.HomeCountry,
            Birthday = from.Birthday,
            Job = from.GetStringCustomField(fields.Job),
            DateOfEntry = from.GetDateCustomField(fields.DateOfEntry)?.ToDateTime(TimeOnly.MinValue),
            DateOfBaptism = from.GetDateCustomField(fields.DateOfBaptism),
            FamilyStatusId = from.MaritalStatus switch
            {
                MaritalStatus.Single => 1,
                MaritalStatus.Married => 2,
                MaritalStatus.Separated => 3,
                MaritalStatus.Divorced => 4,
                MaritalStatus.Widowed => 5,
                MaritalStatus.Engaged => 6,
                _ => 0
            },
            SexId = from.Gender switch
            {
                Gender.Male => 1,
                Gender.Female => 2,
                _ => 0
            },
            OptigemId = from.GivingNumber,
            DateOfDeath = from.GetDateCustomField(fields.DateOfDeath),
            StatusId = settings.Value.Status.GetValueOrDefault(from.CategoryId, settings.Value.DefaultStatusId),
            DepartmentIds = [settings.Value.Departments.GetValueOrDefault(from.CategoryId, settings.Value.DefaultDepartment)],
            CampusId = 0,
            PrivacyPolicyAgreement = new()
            {
                Date = from.GetDateCustomField(fields.ApprovalOfPrivacyPolicy) ?? DateOnly.FromDateTime(DateTime.UtcNow),
                TypeId = 3,
                WhoId = 1
            },
            AdditionalData = await SetCustomFields(from)
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
            // Only delete if synced using this tool, otherwise we might delete manually created entries
            return;
        }

        await churchTools.Persons[additional.Id.Value].DeleteAsync();
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

