using System;
using System.IO;
using System.Text.Json;

namespace ReduxInstaller.Services
{
    public class AppSettings
    {
        public string? GtaVPath { get; set; }
        public string Language { get; set; } = "ru-RU";
    }

    public class SettingsService
    {
        private static SettingsService? _instance;
        private static readonly object _lock = new object();
        private readonly string _settingsFilePath;
        private AppSettings _settings;

        public static SettingsService Instance => _instance ??= new SettingsService();

        public AppSettings Settings => _settings;

        public event EventHandler? SettingsChanged;

        private SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDirectory = Path.Combine(appDataPath, "ReduxInstaller");
            
            try
            {
                if (!Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }
            }
            catch
            {
                // Fallback to temp directory if we can't use AppData
                configDirectory = Path.Combine(Path.GetTempPath(), "ReduxInstaller");
                try
                {
                    if (!Directory.Exists(configDirectory))
                    {
                        Directory.CreateDirectory(configDirectory);
                    }
                }
                catch
                {
                    // Last resort
                    configDirectory = Path.GetTempPath();
                }
            }

            _settingsFilePath = Path.Combine(configDirectory, "settings.json");
            _settings = LoadSettings();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        LoggingService.Instance.Info("Settings loaded successfully");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to load settings", ex);
            }

            // Return default settings if loading fails
            LoggingService.Instance.Info("Using default settings");
            return new AppSettings();
        }

        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
                LoggingService.Instance.Info("Settings saved successfully");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to save settings", ex);
            }
        }

        public void SetGtaVPath(string? path)
        {
            _settings.GtaVPath = path;
            SaveSettings();
        }

        public void SetLanguage(string language)
        {
            _settings.Language = language;
            SaveSettings();
        }

        public string? GetGtaVPath()
        {
            return _settings.GtaVPath;
        }

        public string GetLanguage()
        {
            return _settings.Language;
        }

        public bool IsGtaVPathSet()
        {
            return !string.IsNullOrEmpty(_settings.GtaVPath) && Directory.Exists(_settings.GtaVPath);
        }
    }
}