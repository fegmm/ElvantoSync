namespace ElvantoSync.Settings;

public record ApplicationSettings(
    string OutputFolder,
    string ElvantoKey,
    string NextcloudServer,
    string NextcloudUser,
    string NextcloudPassword,
    string KASLogin,
    string KASAuthData,
    string KASDomain,
    string GroupLeaderSuffix,
    string UploadGroupMailAddressesToNextcloudPath,
    bool LogOnly = false,
    bool SyncElvantoDepartementsToGroups = true,
    bool SyncNextcloudPeople = true,
    bool SyncNextcloudContacts = true,
    bool SyncNextcloudGroups = true,
    bool SyncNextCloudTalk = true,
    bool SyncNextcloudGroupLeaders = true,
    bool SyncNextcloudDeck = true,
    bool SyncNextcloudCollectives = true,
    bool SyncNextcloudGroupmembers = true,
    bool SyncNextcloudGroupfolders = true,
    bool SyncElvantoGroupsToKASMail = true

);
