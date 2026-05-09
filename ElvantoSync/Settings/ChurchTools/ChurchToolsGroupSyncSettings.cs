using System.Collections.Generic;
using Fegmm.ChurchTools.Groups;
using Fegmm.Elvanto.Models;

namespace ElvantoSync.Settings.ChurchTools;

internal record ChurchToolsGroupSyncSettings : SyncSettings
{
    // Elvanto does not have a position field for dbls. So we hack it in as a fixed value
    internal const byte DblPosition = 255;
    internal const string ConfigSection = "Sync:ChurchTools:GroupsToChurchToolsSync";

    public bool IncludeDblsAsMembers { get; set; } = true;
    public int DefaultGroupTypeId { get; set; } = 2;
    public int DefaultGroupStatusId { get; set; } = 1;

    public Dictionary<string, int> GroupCategoryMapping { get; set; } = new()
    {
        ["Leitungsgruppen"] = 2,
        ["Lehre"] = 2,
        ["Jüngerschaft"] = 3,
        ["Junge Gemeinde"] = 1,
        ["Dienstbereich Gottesdienst"] = 4,
        ["Dienstbereich Technik"] = 15,
        ["Dienstbereich Hausverwaltung"] = 5,
        ["Dienstbereich Kommunikation"] = 6,
        ["Dienstbereich Bewirtung"] = 9,
        ["Dienstbereich Finanzen u. Recht"] = 12
    };

    public Dictionary<string, int> GroupTypeMapping { get; set; } = new()
    {
        ["Hauskreise"] = 1,
        ["Leitungsgruppen"] = 13,
    };

    public Dictionary<(int, GroupMemberPositions?), int?> GroupTypeAndRoleToRoleIdMapping { get; set; } = new()
    {
        [(1, null)] = 8,
        [(1, GroupMemberPositions.AssistantLeader)] = 10,
        [(1, GroupMemberPositions.Leader)] = 9,
        [(2, null)] = 15,
        [(2, GroupMemberPositions.AssistantLeader)] = 16,
        [(2, GroupMemberPositions.Leader)] = 16,
        [(2, (GroupMemberPositions)DblPosition)] = 15,
        [(13, null)] = 64,
        [(13, GroupMemberPositions.AssistantLeader)] = 67,
        [(13, GroupMemberPositions.Leader)] = 67
    };

    public Dictionary<string, List<string>> CategoryToDblPersonIdMapping { get; set; } = new()
    {
        ["Lehre"] = ["33156073-e326-4c24-bd80-4966428da053"],
        ["Jüngerschaft"] = ["964d0b45-c38d-464c-bb11-4a3369d9081d"],
        ["Junge Gemeinde"] = ["686b70c8-bc9e-4bea-a34b-241d919d0aa6"],
        ["Dienstbereich Gottesdienst"] = ["f6b1b654-a77c-4f09-857d-2933d25de921"],
        ["Dienstbereich Technik"] = ["f6b1b654-a77c-4f09-857d-2933d25de921", "bc16ea2d-fcec-4088-b81b-781d07d02f7a", "bc16ea2d-fcec-4088-b81b-781d07d02f7a"],
        ["Dienstbereich Hausverwaltung"] = ["f6b1b654-a77c-4f09-857d-2933d25de921", "d145e114-228a-402c-ad62-d693485f67d5", "88502915-40dc-4e8e-9211-9f6c3cd5a3ef"],
        ["Dienstbereich Kommunikation"] = ["f6b1b654-a77c-4f09-857d-2933d25de921", "7644ae78-f40b-44c5-8612-827095e69af2"],
        ["Dienstbereich Bewirtung"] = ["f6b1b654-a77c-4f09-857d-2933d25de921", "01e2e78a-99e4-487c-bb42-4b9ace917502", "c21758e7-c18a-4030-9513-04e7d87b3cd1"],
        ["Dienstbereich Finanzen u. Recht"] = ["f6b1b654-a77c-4f09-857d-2933d25de921", "4145a3bc-6d4a-4700-8864-cc3e4735b12c"]
    };
}
