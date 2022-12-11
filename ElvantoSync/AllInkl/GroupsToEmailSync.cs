using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ServiceReference1;
using SimpleSOAPClient;
using System.Xml;
using System.Xml.Linq;
using KasApi;
using KasApi.Requests;
using ElvantoSync.ElvantoApi.Models;
using KasApi.Response;

namespace ElvantoSync.AllInkl
{
    internal class GroupsToEmailSync : Sync<string, Group, MailForward>
    {
        private readonly ElvantoApi.Client elvanto;
        private readonly Client kas;
        private readonly string domain;

        public GroupsToEmailSync(ElvantoApi.Client elvanto, KasApi.Client kas, string domain)
        {
            this.elvanto = elvanto;
            this.kas = kas;
            this.domain = domain;
        }

        public override async Task<Dictionary<string, Group>> GetFromAsync()
        {
            return (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = new[] { "people" } })).Groups.Group
                .Where(i => i.People?.Person != null && i.People.Person.Any())
                .ToDictionary(i => SanitizeName(i.Name), i => i);
        }

        public override async Task<Dictionary<string, MailForward>> GetToAsync()
        {
            return (await kas.GetMailforwardsAsync())
                .Where(i => i.MailForwardAdress.Split("@")[1] == this.domain)
                .ToDictionary(i => SanitizeName(i.MailForwardAdress.Split("@")[0]), i => i);
        }

        public override async Task AddMissingAsync(Dictionary<string, Group> missing)
        {
            var tasks = missing
                .Select(i => kas.ExecuteRequestWithParams(new AddMailForward() { LocalPart = i.Key, DomainPart = domain, Targets = new[] { "technik@fegmm.de" } }));
            await Task.WhenAll(tasks);
        }

        public override async Task RemoveAdditionalAsync(Dictionary<string, MailForward> additionals)
        {
            var tasks = additionals
                .Select(i => kas.ExecuteRequestWithParams(new DeleteMailForward() { MailForward = $"{i.Key}@{domain}" }));
            await Task.WhenAll(tasks);
        }

        internal static string SanitizeName(string name)
        {
            name = ReplaceSpecialCharacters(name);
            name = name.Replace(" ", "-").Trim('-', '.');
            name = ReplaceDoubleMinusRecursivly(name);
            name = name.ToLower();

            return name;

            static string ReplaceSpecialCharacters(string name)
            {
                var replace = new Dictionary<string, string>()
                {
                    {"ä", "ae"},
                    {"ö", "oe"},
                    {"ü", "ue"},
                    {"Ä", "Ae"},
                    {"Ö", "Oe"},
                    {"Ü", "Ue"},
                    {"ß", "ss" },
                    {"/", "-und-" },
                    {"&", "-und-" },
                    {"+", "-und-" },
                    {",", "-" },
                    {"(", "-" },
                    {")", "-" },
                    {"\"", "" },
                };

                foreach (var item in replace)
                    name = name.Replace(item.Key, item.Value);
                return name;
            }

            static string ReplaceDoubleMinusRecursivly(string name)
            {
                var old_length = 0;
                while (name.Length != old_length)
                {
                    old_length = name.Length;
                    name = name.Replace("--", "-");
                }
                return name;
            }
        }
    }
}
