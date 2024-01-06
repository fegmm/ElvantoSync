﻿using ElvantoSync.ElvantoApi;
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
    class PeopleToNextcloudContactSync(Client elvanto, Settings settings, WebDavClient nextcloud_webdav) : Sync<string, Person, WebDavResource>(settings)
    {
        public override async Task<Dictionary<string, Person>> GetFromAsync()
        {
            return (await elvanto.PeopleGetAllAsync(new GetAllPeopleRequest())).People.Person
                .ToDictionary(i => $"Elvanto-{i.Id}");
        }

        public override async Task<Dictionary<string, WebDavResource>> GetToAsync()
        {
            var people = await GetFromAsync();
            var contact_response = await nextcloud_webdav.Propfind("remote.php/dav/addressbooks/users/Administrator/default/");
            var contacts = contact_response.Resources.ToDictionary(i => i.Uri.Split("/")[^1].Replace(".vcf", ""));
            return contacts
                .Where(i => !people.ContainsKey(i.Key) || DateTime.Parse(people[i.Key].Date_modified) < i.Value.LastModifiedDate.Value.ToUniversalTime())
                .ToDictionary(i => i.Key, i => i.Value);
        }

        public override async Task AddMissingAsync(Dictionary<string, Person> missing)
        {
            using var img_client = new HttpClient();
            var images = (await Task.WhenAll(
                missing.Select(async i => (i.Key, new Photo(
                    true,
                    i.Value.Picture.Split('.')[^1],
                    Convert.ToBase64String(
                        await img_client.GetByteArrayAsync(i.Value.Picture)
                    )
                )))
            )).ToDictionary(i => i.Key, i => i.Item2);
            
            var res = await Task.WhenAll(
                missing.Select(item => nextcloud_webdav.PutFile($"remote.php/dav/addressbooks/users/Administrator/default/{item.Key}.vcf", new StringContent(new VCard()
                {
                    Version = MixERP.Net.VCards.Types.VCardVersion.V4,
                    FirstName = item.Value.Firstname,
                    LastName = item.Value.Lastname,
                    MiddleName = item.Value.Middle_name,
                    FormattedName = $"{item.Value.Lastname}, {item.Value.Firstname}",
                    Emails = new[] { new Email() { EmailAddress = item.Value.Email, Type = MixERP.Net.VCards.Types.EmailType.Smtp } },
                    Telephones = new[] {
                        new Telephone() { Number = item.Value.Phone, Type = MixERP.Net.VCards.Types.TelephoneType.Home},
                        new Telephone() {Number= item.Value.Mobile, Type = MixERP.Net.VCards.Types.TelephoneType.Cell}
                    }.Where(i => !string.IsNullOrWhiteSpace(i.Number)),
                    Photo = images[item.Key].Extension != "svg"?images[item.Key]:null,
                    BirthDay = DateTime.TryParse(item.Value.Birthday, out var bday) ? bday : null
                }.Serialize())))
            );
        }

        public async override Task RemoveAdditionalAsync(Dictionary<string, WebDavResource> additionals)
        {
            await Task.WhenAll(additionals
                .Where(i => !i.Value.IsCollection)
                .Select(i => nextcloud_webdav.Delete(i.Value.Uri))
            );
        }
        public override bool IsActive()
    {
        return settings.SyncNextcloudContacts;
    }
    }
}
