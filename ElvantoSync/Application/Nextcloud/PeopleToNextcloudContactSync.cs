using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using MixERP.Net.VCards;
using MixERP.Net.VCards.Models;
using MixERP.Net.VCards.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebDav;

namespace ElvantoSync.Nextcloud
{
    class PeopleToNextcloudContactSync(Client elvanto, Settings settings, WebDavClient nextcloud_webdav) : Sync<Person, WebDavResource>(settings)
    {
        public override bool IsActive() => settings.SyncNextcloudContacts;
        public override string FromKeySelector(Person i) => $"Elvanto-{i.Id}";
        public override string ToKeySelector(WebDavResource i) => i.Uri.Split("/")[^1].Replace(".vcf", "");

        public override async Task<IEnumerable<Person>> GetFromAsync()
            => (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest())).People.Person;

        public override async Task<IEnumerable<WebDavResource>> GetToAsync()
        {
            var contact_response = await nextcloud_webdav.Propfind("remote.php/dav/addressbooks/users/Administrator/default/");
            return contact_response.Resources.Where(i => ToKeySelector(i).Contains("Elvanto-"));
            //.Where(i => !people.ContainsKey(i.Key) || DateTime.Parse(people[i.Key].Date_modified) < i.Value.LastModifiedDate.Value.ToUniversalTime());
        }

        public override async Task AddMissingAsync(IEnumerable<Person> missing)
        {
            using var img_client = new HttpClient();
            var images = (await Task.WhenAll(
                missing.Select(async i => ("Elvanto-" + i.Id, new Photo(
                    true,
                    i.Picture.Split('.')[^1],
                    Convert.ToBase64String(
                        await img_client.GetByteArrayAsync(i.Picture)
                    )
                )))
            )).ToDictionary(i => i.Item1, i => i.Item2);

            var res = await Task.WhenAll(
                missing.Select(item => nextcloud_webdav.PutFile($"remote.php/dav/addressbooks/users/Administrator/default/{"Elvanto-" + item.Id}.vcf", new StringContent(new VCard()
                {
                    Version = MixERP.Net.VCards.Types.VCardVersion.V4,
                    FirstName = item.Firstname,
                    LastName = item.Lastname,
                    MiddleName = item.Middle_name,
                    FormattedName = $"{item.Lastname}, {item.Firstname}",
                    Emails = new[] { new Email() { EmailAddress = item.Email, Type = MixERP.Net.VCards.Types.EmailType.Smtp } },
                    Telephones = new[] {
                        new Telephone() { Number = item.Phone, Type = MixERP.Net.VCards.Types.TelephoneType.Home},
                        new Telephone() {Number= item.Mobile, Type = MixERP.Net.VCards.Types.TelephoneType.Cell}
                    }.Where(i => !string.IsNullOrWhiteSpace(i.Number)),
                    Photo = images["Elvnato-" + item.Id].Extension != "svg" ? images["Elvnato-" + item.Id] : null,
                    BirthDay = DateTime.TryParse(item.Birthday, out var bday) ? bday : null
                }.Serialize())))
            );
        }

        public async override Task RemoveAdditionalAsync(IEnumerable<WebDavResource> additionals)
        {
            await Task.WhenAll(additionals
                .Where(i => !i.IsCollection)
                .Select(i => nextcloud_webdav.Delete(i.Uri))
            );
        }
    }
}
