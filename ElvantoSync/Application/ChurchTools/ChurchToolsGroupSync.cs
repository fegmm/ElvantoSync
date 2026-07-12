using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElvantoSync.ElvantoService;
using ElvantoSync.Extensions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.ChurchTools;
using Fegmm.ChurchTools;
using Fegmm.ChurchTools.Groups;
using Fegmm.ChurchTools.Groups.Item.Members.Item;
using Fegmm.ChurchTools.Groups.Members;
using Fegmm.Elvanto.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElvantoSync.ChurchTools;

using ChurchToolsGroup = GroupsGetResponse_data;

internal class ChurchToolsGroupSync(
    IElvantoClient elvantoClient,
    ChurchToolsClient churchToolsClient,
    DbContext dbContext,
    IOptions<ChurchToolsGroupSyncSettings> settings,
    ILogger<ChurchToolsGroupSync> logger
) : Sync<Group, ChurchToolsGroup>(dbContext, settings, logger)
{
    private List<MembersGetResponse_data> allMembers;

    public override string FallbackFromKeySelector(Group i)
        => i.Name;

    public override string FallbackToKeySelector(ChurchToolsGroup i)
        => i.Name;

    public override string FromKeySelector(Group i)
        => i.Id;

    public override string ToKeySelector(ChurchToolsGroup i)
        => i.Id.ToString();

    public override async Task<IEnumerable<Group>> GetFromAsync()
        => await elvantoClient.GroupsGetAllAsync(new()
        {
            Fields = [
                GroupAdditionalFields.Categories,
                GroupAdditionalFields.People,
            ],
        });

    public override async Task<IEnumerable<ChurchToolsGroup>> GetToAsync()
    {
        List<ChurchToolsGroup> allGroups = new();

        int pages = 1;
        for (int i = 1; i <= pages; i++)
        {
            var listResponse = await churchToolsClient.Groups.GetAsGroupsGetResponseAsync(conf =>
            {
                conf.QueryParameters.Limit = 200;
                conf.QueryParameters.Page = i;
            });
            pages = listResponse.Meta.Pagination.LastPage.Value;
            allGroups.AddRange(listResponse.Data);
        }

        var allMembersResponse = await churchToolsClient.Groups.Members.GetAsMembersGetResponseAsync();
        allMembers = allMembersResponse.Data;

        return allGroups;
    }

    protected override async Task<string> AddMissing(Group missing)
    {
        var mappedCategoryId = missing.Categories?.Category
            ?.FirstOrDefault(cat => settings.Value.GroupCategoryMapping.ContainsKey(cat?.Name))
            ?.Name;

        var mappedGroupTypeId = missing.Categories?.Category
            ?.FirstOrDefault(cat => settings.Value.GroupTypeMapping.ContainsKey(cat?.Name))
            ?.Name;

        var createGroupResponse = await churchToolsClient.Groups.PostAsGroupsPostResponseAsync(new()
        {
            Name = missing.Name,
            GroupCategoryId = mappedCategoryId != null ? settings.Value.GroupCategoryMapping[mappedCategoryId] : null,
            GroupTypeId = mappedGroupTypeId != null ? settings.Value.GroupTypeMapping[mappedGroupTypeId] : settings.Value.DefaultGroupTypeId,
            GroupStatusId = settings.Value.DefaultGroupStatusId,
            Visibility = GroupsPostRequestBody_visibility.Restricted,
        });

        await UpdateSyncNote(null, missing.Id, createGroupResponse.Data.Id.Value);

        return createGroupResponse.Data.Id.ToString();
    }

    protected override async Task UpdateMatch(Group from, ChurchToolsGroup to)
    {
        var patchPayload = new Dictionary<string, object>();

        if (from.Name != to.Name)
        {
            patchPayload["name"] = from.Name;
        }

        if (patchPayload.Any())
        {
            await churchToolsClient.Groups[to.Id.Value].PatchAsWithGroupPatchResponseAsync(new()
            {
                AdditionalData = new Dictionary<string, object>()
                {
                    ["name"] = from.Name,
                },
            });
        }

        try
        {
            await SyncMembers(from, to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync members for group with ID {GroupId}", to.Id);
        }

        var syncNote = await GetSyncNote(to.Id.Value);
        await UpdateSyncNote(syncNote?.Id, from.Id, to.Id.Value);
    }

    private async Task SyncMembers(Group from, ChurchToolsGroup to)
    {
        List<Exception> exceptions = [];
        var dbls = settings.Value.IncludeDblsAsMembers && from.Categories?.Category != null ? from.Categories.Category.SelectMany(cat => settings.Value.CategoryToDblPersonIdMapping.GetValueOrDefault(cat?.Name, [])) : [];
        List<GroupMember> elvantoMembers = [..from.People.Person ?? [], ..dbls.Select(dbl => new GroupMember()
        {
            Id = dbl
        })];
        elvantoMembers = elvantoMembers.DistinctBy(m => m.Id).ToList();

        var ctIdsOfPersons = elvantoMembers
            .ToDictionary(p => p.Id, p => dbContext.ElvantoToChurchToolsPeopleId(p.Id))
            .Where(kv => kv.Value != null)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var ctMembers = allMembers.Where(m => m.GroupId == to.Id.Value).ToList();
        var memberComparison = elvantoMembers
            .Where(p => ctIdsOfPersons.ContainsKey(p.Id))
            .CompareTo(ctMembers, p => ctIdsOfPersons.GetValueOrDefault(p.Id), m => m.PersonId.ToString());

        int? GetMemberRoleId(string elvantoId, int groupTypeId, GroupMemberPositions? position)
        {
            position = dbls.Contains(elvantoId) ? (GroupMemberPositions)ChurchToolsGroupSyncSettings.DblPosition : position;
            return settings.Value.GroupTypeAndRoleToRoleIdMapping.GetValueOrDefault((groupTypeId, position), null);
        }

        // Add missing members
        foreach (var missingMember in memberComparison.additional)
        {
            try
            {
                var ctPersonId = int.Parse(ctIdsOfPersons.GetValueOrDefault(missingMember.Id));
                await churchToolsClient.Groups[to.Id.Value].Members[ctPersonId].PutAsMemberPutResponseAsync(new()
                {
                    IgnoreGroupFull = true,
                    GroupMemberStatus = new() { String = "active" },
                    InformLeader = false,
                    OnlyAdd = true,
                    GroupTypeRoleId = GetMemberRoleId(missingMember.Id, to.Information.GroupTypeId.Value, missingMember.Position),
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add member with Elvanto ID {ElvantoId} to group with ChurchTools ID {GroupId}", missingMember.Id, to.Id);
                exceptions.Add(ex);
            }
        }

        // Remove additional members
        foreach (var additionalMember in memberComparison.missing)
        {
            try
            {
                await churchToolsClient.Groups[to.Id.Value].Members[additionalMember.PersonId.Value].DeleteAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove member with ID {PersonId} from group with ChurchTools ID {GroupId}", additionalMember.PersonId, to.Id);
                exceptions.Add(ex);
            }
        }

        // Update changed members
        foreach (var match in memberComparison.matches)
        {
            try
            {
                var fromMember = match.Item1;
                var toMember = match.Item2;
                var targetedRoleId = GetMemberRoleId(fromMember.Id, to.Information.GroupTypeId.Value, fromMember.Position);

                if (toMember.GroupTypeRoleId != targetedRoleId)
                {
                    await churchToolsClient.Groups[to.Id.Value].Members[toMember.PersonId.Value].PatchAsMemberPatchResponseAsync(new()
                    {
                        InformLeader = false,
                        GroupTypeRoleId = targetedRoleId,
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update member with Person ID {PersonId} in group with ChurchTools ID {GroupId}", match.Item2.PersonId, to.Id);
                exceptions.Add(ex);
            }
        }
    }

    protected override async Task RemoveAdditional(ChurchToolsGroup additional)
    {
        var syncNote = await GetSyncNote(additional.Id.Value);

        // Delete only groups created by elvanto sync
        if (syncNote != null)
        {
            await churchToolsClient.Groups[additional.Id.Value].DeleteAsync();
        }
    }

    private async Task<Fegmm.ChurchTools.Notes.Item.Item.WithDomainGetResponse_data> GetSyncNote(int churchToolsId)
    {
        return (await churchToolsClient.Notes["group"][churchToolsId].GetAsWithDomainGetResponseAsync())
            .Data
            .FirstOrDefault(n => n.Text.StartsWith($"Elvanto Sync Note:"));
    }

    private static string GetSyncNoteText(string fromId)
        => $@"Elvanto Sync Note:
                - Elvanto ID: {fromId}
                - Last Updated: {DateTime.UtcNow}";

    private async Task UpdateSyncNote(int? syncNoteId, string from, int toId)
    {
        try
        {
            if (syncNoteId != null)
            {
                await churchToolsClient.Notes["group"][toId][syncNoteId.Value].PutAsWithNotePutResponseAsync(new()
                {
                    Text = GetSyncNoteText(from),
                    CommentViewerId = 2, // Admin only
                });
            }
            else
            {
                await churchToolsClient.Notes["group"][toId].PostAsWithDomainPostResponseAsync(new()
                {
                    Text = GetSyncNoteText(from),
                    CommentViewerId = 2, // Admin only
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update sync note for grouop with ID {GroupId}", toId);
        }
    }
}
