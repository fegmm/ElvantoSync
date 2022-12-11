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
using static ElvantoSync.AllInkl.GroupsToEmailSync;
using System.Reflection;
using System.Globalization;

namespace ElvantoSync.AllInkl
{
    internal class GroupMembersToMailForwardMemberSync : Sync<(string groupName, string userMail), GroupMember, MailForward>
    {
        private readonly ElvantoApi.Client elvanto;
        private readonly Client kas;
        private readonly string domain;

        public GroupMembersToMailForwardMemberSync(ElvantoApi.Client elvanto, KasApi.Client kas, string domain)
        {
            this.elvanto = elvanto;
            this.kas = kas;
            this.domain = domain;
        }

        public override async Task<Dictionary<(string groupName, string userMail), GroupMember>> GetFromAsync()
        {
            return (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = new[] { "people" } })).Groups.Group
                .Where(i => i.People != null && i.People.Person != null)
                .SelectMany(i => i.People.Person.Select(j => (i.Name, j)))
                .DistinctBy(i => (SanitizeName(i.Name), i.j.Email.ToLower()))
                .Where(i => !string.IsNullOrWhiteSpace(i.j.Email))
                .ToDictionary(i => (SanitizeName(i.Name), i.j.Email.ToLower()), i => i.j); // somehow KAS lowers all target emails
        }

        public override async Task<Dictionary<(string groupName, string userMail), MailForward>> GetToAsync()
        {
            var members = new List<(string group, string user)>();
            var elvantoGroups = (await elvanto.GroupsGetAllAsync(new GetAllRequest())).Groups.Group.Select(i => SanitizeName(i.Name));
            return (await kas.GetMailforwardsAsync())
                .Where(i => i.MailForwardAdress.Split("@")[1] == this.domain)
                .Select(i => (Name: i.MailForwardAdress.Split("@")[0], MailForward: i))
                .Where(i => elvantoGroups.Contains(i.Name))
                .SelectMany(i => i.MailForward.MailForwardTargets.Select(j => (i, mail: j)))
                .Distinct()
                .ToDictionary(i => (i.i.Name, i.mail), i => i.i.MailForward);
        }

        public override async Task AddMissingAsync(Dictionary<(string groupName, string userMail), GroupMember> missing)
        {
            var groups = (await kas.GetMailforwardsAsync())
                .Where(i => i.MailForwardAdress.Split("@")[1] == this.domain)
                .ToDictionary(i => SanitizeName(i.MailForwardAdress.Split("@")[0]), i => i);

            var tasks = missing
                .GroupBy(i => i.Key.groupName)
                .Select(i => kas.ExecuteRequestWithParams(
                    new UpdateMailForward() { 
                        MailForward = $"{i.Key}@{this.domain}",
                        Targets = i
                            .Select(j => j.Key.userMail)
                            .Concat(groups[i.Key].MailForwardTargets)
                            .DistinctBy(i => i.ToLower())
                            .ToArray()
                    })
                )
                .ToArray();

            await Task.WhenAll(tasks);
        }

        public override async Task RemoveAdditionalAsync(Dictionary<(string groupName, string userMail), MailForward> additionals)
        {
            var groups = (await kas.GetMailforwardsAsync())
                .Where(i => i.MailForwardAdress.Split("@")[1] == this.domain)
                .ToDictionary(i => SanitizeName(i.MailForwardAdress.Split("@")[0]), i => i);

            var tasks = additionals
                .GroupBy(i => i.Key.groupName)
                .Select(i => kas.ExecuteRequestWithParams(
                    new UpdateMailForward()
                    {
                        MailForward = $"{i.Key}@{this.domain}",
                        Targets = groups[i.Key].MailForwardTargets
                            .Except(i.Select(j => j.Key.userMail))
                            .ToArray()
                    })
                )
                .ToArray();

            await Task.WhenAll(tasks);
        }
    }
}
