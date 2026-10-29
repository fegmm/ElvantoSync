using ElvantoSync.ChurchTools;
using ElvantoSync.Persistence;
using ElvantoSync.Settings;
using ElvantoSync.Settings.Nextcloud;
using ElvantoSync.ChurchTools.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixERP.Net.VCards;
using MixERP.Net.VCards.Models;
using MixERP.Net.VCards.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using WebDav;

namespace ElvantoSync.Nextcloud;

class PeopleToNextcloudContactSync(
    IChurchToolsSource churchTools,
    IOptions<PeopleToNextcloudSyncSettings> peopleSettings,
    IOptions<ApplicationSettings> applicationSettings,
    WebDavClient nextcloud_webdav,
    HttpClient img_client,
    DbContext dbContext,
    IOptions<PeopleToContactSyncSettings> settings,
    ILogger<PeopleToNextcloudContactSync> logger
) : Sync<Person, WebDavResource>(dbContext, settings, logger)
{
    public override string FromKeySelector(Person i) => i.Id;
    public override string ToKeySelector(WebDavResource i) => i.Uri;
    public override string FallbackFromKeySelector(Person i) => peopleSettings.Value.IdPrefix + i.Id + ".vcf";
    public override string FallbackToKeySelector(WebDavResource i) => i.Uri.Split("/")[^1];

    public override async Task<IEnumerable<Person>> GetFromAsync()
        => (await churchTools.GetPeopleAsync());

    public override async Task<IEnumerable<WebDavResource>> GetToAsync()
    {
        var contact_response = await nextcloud_webdav.Propfind($"remote.php/dav/addressbooks/users/{applicationSettings.Value.NextcloudUser}/{settings.Value.ContactBook}/");
        return contact_response.Resources.Where(i => ToKeySelector(i).Contains(peopleSettings.Value.IdPrefix));
    }

    protected override async Task<string> AddMissing(Person person)
    {
        VCard vcard = await PersonToVCard(person);

        string uri = $"remote.php/dav/addressbooks/users/{applicationSettings.Value.NextcloudUser}/{settings.Value.ContactBook}/{peopleSettings.Value.IdPrefix + person.Id}.vcf";
        var res = await nextcloud_webdav.PutFile(uri, new StringContent(vcard.Serialize()));
        if (!res.IsSuccessful)
        {
            throw new Exception($"Add WebDav contact returned non success code {res.StatusCode} with message: {res.Description}");
        }

        return uri;
    }

    protected async override Task RemoveAdditional(WebDavResource additional)
    {
        if (additional.IsCollection)
        {
            return;
        }

        var res = await nextcloud_webdav.Delete(additional.Uri);
        if (!res.IsSuccessful)
        {
            throw new Exception($"Delete WebDav contact returned non success code {res.StatusCode} with message: {res.Description}");
        }
    }

    protected override async Task UpdateMatch(Person person, WebDavResource vCard)
    {
        if (person.LastEditedDate <= vCard.LastModifiedDate)
        {
            return;
        }

        var vcard = await PersonToVCard(person);

        var res = await nextcloud_webdav.PutFile(vCard.Uri, new StringContent(vcard.Serialize()));
        if (!res.IsSuccessful)
        {
            throw new Exception($"Update WebDav contact returned non success code {res.StatusCode} with message: {res.Description}");
        }
    }

    private async Task<VCard> PersonToVCard(Person person)
    {
        Photo photo = null;
        if (!string.IsNullOrWhiteSpace(person.ImageUrl))
        {
            try
            {
                byte[] personPhoto = await img_client.GetByteArrayAsync(person.ImageUrl);
                var photoExtension = Path.GetExtension(new Uri(person.ImageUrl).AbsolutePath).TrimStart('.');
                if (personPhoto.Length > 0 && !string.IsNullOrWhiteSpace(photoExtension))
                {
                    photo = new Photo(true, photoExtension, Convert.ToBase64String(personPhoto));
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or UriFormatException)
            {
                logger.LogWarning(ex, "Could not download picture for person {PersonId}; creating contact without a photo", person.Id);
            }
        }

        return new VCard()
        {
            Version = MixERP.Net.VCards.Types.VCardVersion.V4,
            FirstName = person.FirstName,
            LastName = person.LastName,
            FormattedName = $"{person.LastName}, {person.FirstName}",
            Emails = new[] { new Email() { EmailAddress = person.Email, Type = MixERP.Net.VCards.Types.EmailType.Smtp } },
            Telephones = (new[] {
                            new Telephone() { Number = person.PhonePrivate, Type = MixERP.Net.VCards.Types.TelephoneType.Home},
                            new Telephone() { Number= person.Mobile, Type = MixERP.Net.VCards.Types.TelephoneType.Cell}
                    }).Where(i => !string.IsNullOrWhiteSpace(i.Number)),
            Photo = photo?.Extension != "svg" ? photo : null,
            BirthDay = person.Birthday
        };
    }
}
