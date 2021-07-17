# ElvantoSync
Offers a synchronisation from Elvanto to other applications

## Configuration
Configuration is done by environment variables.

| Environment Variable                | Type    | Description                                                                  | Default |
|-------------------------------------|---------|------------------------------------------------------------------------------|---------|
| ELVANTO_API_KEY                     | string  | Key for the Elvanto api                                                      |         |
| NEXTCLOUD_SERVER                    | string  | Nextcloud Server Address                                                     |         |
| SYNC_ELVANTO_DEPARTEMENTS_TO_GROUPS | boolean | Whether to sync Elvanto departements and sub-departements to Elvanto groups  | false   |
| SYNC_NEXTCLOUD_PEOPLE               | boolean | Whether to sync Elvanto people to Nextcloud accounts                         | false   |
| SYNC_NEXTCLOUD_GROUPS               | boolean | Whether to sync Elvanto groups to Nextcloud groups                           | false   |
| SYNC_NEXTCLOUD_GROUPMEMBERS         | boolean | Whether to sync Elvanto group members to Nextcloud group members             | false   |
| SYNC_NEXTCLOUD_GROUPFOLDERS         | boolean | Whether to sync Elvanto groups to Nextcloud groupfolders                     | false   |
