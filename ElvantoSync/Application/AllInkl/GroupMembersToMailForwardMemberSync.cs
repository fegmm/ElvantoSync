using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using KasApi;
using KasApi.Requests;
using KasApi.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ElvantoSync.Application.AllInkl.GroupsToEmailSync;

namespace ElvantoSync.Application.AllInkl;

internal class GroupMembersToMailForwardMemberSync(IElvantoClient elvanto, IKasClient kas, Settings settings) : Sync<(string groupName, string userMail), GroupMember, MailForward>(settings)
{
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
            .Where(i => i.MailForwardAdress.Split("@")[1] == Settings.KASDomain)
            .Select(i => (Name: i.MailForwardAdress.Split("@")[0], MailForward: i))
            .Where(i => elvantoGroups.Contains(i.Name))
            .SelectMany(i => i.MailForward.MailForwardTargets.Select(j => (i, mail: j)))
            .Distinct()
            .ToDictionary(i => (i.i.Name, i.mail), i => i.i.MailForward);
    }

    public override async Task AddMissingAsync(Dictionary<(string groupName, string userMail), GroupMember> missing)
    {
        var groups = (await kas.GetMailforwardsAsync())
            .Where(i => i.MailForwardAdress.Split("@")[1] == Settings.KASDomain)
            .ToDictionary(i => SanitizeName(i.MailForwardAdress.Split("@")[0]), i => i);

        var tasks = missing
            .GroupBy(i => i.Key.groupName)
            .Select(i => kas.ExecuteRequestWithParams(
                new UpdateMailForward()
                {
                    MailForward = $"{i.Key}@{Settings.KASDomain}",
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
            .Where(i => i.MailForwardAdress.Split("@")[1] == Settings.KASDomain)
            .ToDictionary(i => SanitizeName(i.MailForwardAdress.Split("@")[0]), i => i);

        var tasks = additionals
            .GroupBy(i => i.Key.groupName)
            .Select(i => kas.ExecuteRequestWithParams(
                new UpdateMailForward()
                {
                    MailForward = $"{i.Key}@{Settings.KASDomain}",
                    Targets = groups[i.Key].MailForwardTargets
                        .Except(i.Select(j => j.Key.userMail))
                        .ToArray()
                })
            )
            .ToArray();

        await Task.WhenAll(tasks);
    }
    public override bool IsActive()
    {
        return settings.SyncElvantoGroupsToKASMail;
    }
}
