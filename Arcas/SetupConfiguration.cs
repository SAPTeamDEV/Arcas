using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace Arcas
{
    [Obsolete("Use SetupConfigurationManager.State instead")]
    public class SetupConfiguration
    {
        public string InstallationPath { get; set; } = "";
        public string[] SelectedComponents { get; set; } = Array.Empty<string>();
        public bool LicenseAccepted { get; set; } = false;
        public DateTime InstallationDate { get; set; } = DateTime.Now;

        public static SetupConfiguration Load()
        {
            // For backward compatibility, create from new system
            var state = SetupConfigurationManager.State;
            return new SetupConfiguration
            {
                InstallationPath = state.InstallationPath,
                SelectedComponents = state.SelectedComponentIds.ToArray(),
                LicenseAccepted = state.LicenseAccepted,
                InstallationDate = state.InstallationStartTime
            };
        }

        public void Save()
        {
            // No longer saves to disk - state is runtime only
            // Configuration comes from external sources
            System.Diagnostics.Debug.WriteLine("SetupConfiguration.Save() is deprecated - runtime state is not persisted");
        }

        private static string GetConfigPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "Arcas", "setup.json");
        }

        public bool IsAlreadyInstalled()
        {
            return !string.IsNullOrEmpty(InstallationPath) && 
                   Directory.Exists(InstallationPath) && 
                   LicenseAccepted;
        }
    }
}