using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

namespace QuickFileMarker.Configuration
{
    internal static class QuickFileMarkerConfigurationManager
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickFileMarker");
        private static readonly string TempMarkerFolder = Path.Combine(Path.GetTempPath(), "FileMarkers");

        private static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "extention-config.json");
        private static readonly string IncrementalIdFilePath = Path.Combine(AppDataFolder, "incremental-id.json");

        public static string GetTempMarkerFolder()
        {
            if (!Directory.Exists(TempMarkerFolder))
            {
                Directory.CreateDirectory(TempMarkerFolder);
            }
            return TempMarkerFolder;
        }

        public static ConfigurationRecord LoadConfiguration()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonConvert.DeserializeObject<ConfigurationRecord>(json) ?? new ConfigurationRecord();
                }
                catch
                {
                    return new ConfigurationRecord();
                }
            }

            var defaultConfig = new ConfigurationRecord();
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            return defaultConfig;
        }

        public static int GetNextIdentifier()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            IncrementalIdentifierRecord record = new IncrementalIdentifierRecord { LastIdentifier = 0 };
            if (File.Exists(IncrementalIdFilePath))
            {
                try
                {
                    string json = File.ReadAllText(IncrementalIdFilePath);
                    record = JsonConvert.DeserializeObject<IncrementalIdentifierRecord>(json) ?? record;
                }
                catch { }
            }

            record.LastIdentifier++;

            File.WriteAllText(IncrementalIdFilePath, JsonConvert.SerializeObject(record, Formatting.Indented));

            return record.LastIdentifier;
        }

        public static void CleanUpTempDirectory(ConfigurationRecord config)
        {
            if (!Directory.Exists(TempMarkerFolder)) return;

            try
            {
                var dirInfo = new DirectoryInfo(TempMarkerFolder);
                var files = dirInfo.GetFiles("*.json").OrderBy(f => f.CreationTime).ToList();

                DateTime threshold = DateTime.Now.AddDays(-config.MarkerFileLifetimeInDays);

                // Remove files older than lifetime
                var oldFiles = files.Where(f => f.CreationTime < threshold).ToList();
                foreach (var file in oldFiles)
                {
                    try { file.Delete(); } catch { }
                    files.Remove(file);
                }

                // If still more than max count, remove oldest
                if (files.Count > config.MaxMarkerFileCount)
                {
                    int toRemove = files.Count - config.MaxMarkerFileCount;
                    for (int i = 0; i < toRemove; i++)
                    {
                        try { files[i].Delete(); } catch { }
                    }
                }
            }
            catch { }
        }
    }
}
