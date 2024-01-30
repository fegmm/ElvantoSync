namespace ElvantoSync.Settings.Nextcloud;

internal record GroupsToTalkSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:GroupsToTalkSync";

    public string GroupChatDescription { get; init; } = "Euer Gruppenchat für's Team!\nAnmerkungen: Neue Mitarbeiter, die ihr in Elvanto hinzufügt, haben am nächsten Tag automatisch Zugriff. Um wie bei WhatsApp über jede neue Nachricht ein Push zu erhalten, stellt die Benachrichtigungseinstellungen auf Alle Nachrichten.";
}
