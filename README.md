# ElvantoSync
Offers a synchronisation from Elvanto to other applications

## Configuration
```json
{
  "Application": {
    "ConnectionString": "Data Source=ElvantoSync.db",
    "ElvantoKey": "",
    "KASLogin": "",
    "KASAuthData": "",
    "NextcloudServer": "http://localhost:8080",
    "NextcloudUser": "admin",
    "NextcloudPassword": "StrongPassword123!"
  },
  "Sync": {
    "AllInkl": {
      "GroupsToEmailSync": {
        "LogOnly": true,
        "KASDomain": "sync.example.org",
        "UploadGroupMailAddressesToNextcloudPath": "Documents/Gruppen-Mail-Adressen.pdf"
      }
    },
    "Elvanto": {
      "DepartementsToGroupMemberSync": {
        "LogOnly": true
      }
    },
    "Nextcloud": {
      "GroupsToCollectiveSync": {
        "LogOnly": true
      },
      "GroupsToDeckSync": {
        "LogOnly": true
      },
      "GroupsToNextcloudGroupFolderSync": {
        "LogOnly": true
      },
      "GroupsToNextcloudSync": {
        "LogOnly": true,
        "GroupLeaderSuffix": "- Leitung"
      },
      "GroupsToTalkSync": {
        "LogOnly": true
      },
      "PeopleToContactSync": {
        "LogOnly": true
      },
      "PeopleToNextcloudSync": {
        "LogOnly": true
      }
    }
  }
}
```