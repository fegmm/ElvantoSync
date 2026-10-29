# ElvantoSync
Synchronizes people and groups from ChurchTools to Nextcloud and All-Inkl.
ChurchTools is the source of truth; Elvanto and GroupFinder integrations have been removed.

Sync definitions live in [Application](ElvantoSync/Application). The ChurchTools
source adapter, interface, and models live in
[ChurchToolsSource](ElvantoSync/ChurchToolsSource).

## Configuration
```json
{
  "Application": {
    "ConnectionString": "Data Source=ElvantoSync.db",
    "ChurchToolsUrl": "https://example.church.tools/api",
    "ChurchToolsToken": "",
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
        "LogOnly": true,
        "IncludedAreaIds": []
      },
      "PeopleToNextcloudSync": {
        "LogOnly": true,
        "IncludedAreaIds": []
      }
    }
  }
}
```

The ChurchTools API token needs read access to persons, groups, group memberships,
and group roles. People are fetched with pagination; archived and deceased people
are excluded. Groups synchronize active, non-deleted memberships of those people.
Leadership comes from ChurchTools roles with `isLeader = true`. As before, only
groups with members are synchronized. Group email targets come from the persons'
ChurchTools email addresses.

Set `Sync:Nextcloud:PeopleToNextcloudSync:IncludedAreaIds` for account provisioning
and `Sync:Nextcloud:PeopleToContactSync:IncludedAreaIds` for contacts. Each setting
accepts ChurchTools person area IDs, for example `[1, 3]`, and includes people
in at least one of those areas. An empty array includes all areas. The source
reads these IDs from ChurchTools' `departmentIds` field. The account and contact
filters are independent; group syncs keep their existing source scope.
If `DeleteAdditionals` is enabled for the corresponding sync, managed contacts
and accounts outside its selected areas are eligible for removal. Accounts
remain protected by the existing empty-account check.

People sync runs before group sync and dependent Nextcloud syncs. Existing user and
group mappings supply Nextcloud IDs for memberships, Deck boards, group folders,
Collectives, and Talk. New users use `IdPrefix + ChurchTools person ID`; new groups
use the ChurchTools group ID. Keep the existing `PeopleToNextcloudSync.IdPrefix`
when migrating: its default `Elvanto-` remains for compatibility with existing
Nextcloud accounts and contact files. This prefix identifies managed target
objects and does not select the source system. `ExcludedFromIds` now refers to
ChurchTools IDs; update configured exclusions manually. Target exclusions stay
unchanged. Remove obsolete `Sync:ChurchTools`, `Sync:Elvanto`, GroupFinder options,
and `Application:ElvantoKey` from deployment configuration.

## Validation

```bash
dotnet test ElvantoSync.Test/ElvantoSync.Test.csproj --filter 'FullyQualifiedName~ChurchToolsSourceTests|FullyQualifiedName~ChurchToolsMembershipTests|FullyQualifiedName~PeopleToNextcloudAreaTests|FullyQualifiedName~PeopleToContactAreaTests'
```

On Arch Linux, add `--runtime linux-x64` to the project-level .NET commands if the
SDK otherwise chooses `arch-x64`. The existing Nextcloud integration tests need
Docker; the focused tests above run without external services.
