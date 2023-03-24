﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync
{
    record Settings(
        string OutputFolder,
        string ElvantoKey,
        string NextcloudServer,
        string NextcloudUser,
        string NextcloudPassword,
        string KASLogin,
        string KASAuthData,
        string KASDomain,
        bool LogOnly,
        bool SyncElvantoDepartementsToGroups,
        bool SyncNextcloudPeople,
        bool SyncNextcloudGroups,
        bool SyncNextcloudGroupmembers,
        bool SyncNextcloudGroupfolders,
        bool SyncElvantoGroupsToKASMail,
        string UploadGroupMailAddressesToNextcloudPath);
}            
