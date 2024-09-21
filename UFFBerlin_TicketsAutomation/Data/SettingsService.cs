using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UFFBerlin_TicketsAutomation.Data
{
    public class SettingsService
    {
        private const string SettingsFilePath = "settings.json";

        public SettingsModel Settings { get; private set; }

        public SettingsService()
        {
            // Load settings from the file or create default settings
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                Settings = JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
            }
            else
            {
                Settings = new SettingsModel(); // Initialize with default values
            }
        }

        public async Task SaveSettingsAsync()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }

        public Task UpdateSettingsAsync(SettingsModel updatedSettings)
        {
            Settings = updatedSettings;
            return SaveSettingsAsync();
        }

        public void ReloadSettings()
        {
            // Reload settings from the file dynamically
            LoadSettings();
        }
    }
    public class SettingsModel
    {
        public string SourceFolderId { get; set; } = string.Empty;
        public string DestinationFolderId { get; set; } = string.Empty;
        public string ArchiveFolderId { get; set; } = string.Empty;
        public string EmailSubject { get; set; } = "Default Subject";
        public string EmailTextHtml { get; set; } = "<p>Default Email Body</p>";
    }

}
