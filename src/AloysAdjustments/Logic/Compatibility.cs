﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AloysAdjustments.Configuration;
using AloysAdjustments.Utility;

namespace AloysAdjustments.Logic
{
    public class Compatibility
    {
        public static async Task CleanupOldVersions()
        {
            foreach (var fileName in IoC.Config.OldVersionsToDelete)
            {
                var path = Path.Combine(Configs.GamePackDir, fileName);

                await FileBackup.CleanupBackups(path);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
