# ElvantoSync
Offers a synchronisation from Elvanto to other applications

## Configuration
Configuration is done by environment variables.

| Environment Variable                | Type    | Description                                                                  | Default |
|-------------------------------------|---------|------------------------------------------------------------------------------|---------|
| ELVANTO_API_KEY                     | string  | Key for the Elvanto api                                                      |         |
| NEXTCLOUD_SERVER                    | string  | Nextcloud Server Address                                                     |         |
| NEXTCLOUD_USER                      | string  | The User for the API-Login                                                   |         |
| NEXTCLOUD_PASSWORD                  | string  | The Password for the User for the API-Login                                  |         |
| SYNC_ELVANTO_DEPARTEMENTS_TO_GROUPS | boolean | Whether to sync Elvanto departements and sub-departements to Elvanto groups  | false   |
| SYNC_NEXTCLOUD_PEOPLE               | boolean | Whether to sync Elvanto people to Nextcloud accounts                         | false   |
| SYNC_NEXTCLOUD_GROUPS               | boolean | Whether to sync Elvanto groups to Nextcloud groups                           | false   |
| SYNC_NEXTCLOUD_GROUP_LEADER         | boolean | Whether to sync Elvanto groups leaders to Nextcloud groups                   | false   |
| GROUP_LEADER_SUFFIX                 | string  | Suffix to append, to the Group Name to generate the group leader group       |         |
| SYNC_NEXTCLOUD_GROUPMEMBERS         | boolean | Whether to sync Elvanto group members to Nextcloud group members             | false   |
| SYNC_NEXTCLOUD_GROUPFOLDERS         | boolean | Whether to sync Elvanto groups to Nextcloud groupfolders                     | false   |
| OUTPUT_FOLDER                       | string  | Location for the log files                                                   |         |
| LOG_ONLY                            | boolean | Whether only log files should be created in the output-folder                | false   |
| SYNC_ELVANTO_GROUPS_TO_KAS_MAIL     | string  | Whether to sync Elvanto group to ALLINKL KAS forwarding lists                | false   |
| KAS_LOGIN                           | string  | KAS Username                                                                 |         |
| KAS_AUTH_DATA                       | string  | KAS Password                                                                 |         |
| KAS_DOMAIN                          | string  | Domain for which the forwarding lists are created                            |         |
| UPLOAD_GROUP_MAIL_ADDRESSES_TO_NEXTCLOUD_PATH | string | If set, a PDF file with a table of the mappings from group names to usernames will be uploaded to the nextcloud user's path as specified | |
