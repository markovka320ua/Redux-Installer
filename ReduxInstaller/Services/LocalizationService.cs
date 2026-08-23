using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace ReduxInstaller.Services
{
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        private Dictionary<string, string>? _strings;
        private CultureInfo _currentCulture;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event EventHandler? LanguageChanged;

        public string CurrentLanguage => _currentCulture.Name;

        private LocalizationService()
        {
            var savedLang = SettingsService.Instance.GetLanguage();
            if (string.IsNullOrEmpty(savedLang))
            {
                savedLang = "uk-UA";
            }

            try
            {
                _currentCulture = CultureInfo.GetCultureInfo(savedLang);
            }
            catch
            {
                _currentCulture = CultureInfo.GetCultureInfo("uk-UA");
            }

            LoadResources();
        }

        private void LoadResources()
        {
            try
            {
                var cultureName = _currentCulture.Name;
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                var candidatePaths = new List<string>
                {
                    Path.Combine(baseDir, "Resources", "Localization", cultureName, "strings.json"),
                    Path.Combine(baseDir, "..", "..", "Resources", "Localization", cultureName, "strings.json"),
                    Path.Combine(baseDir, "Resources", "Localization", "uk-UA", "strings.json")
                };

                string? foundFile = null;
                foreach (var path in candidatePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        foundFile = fullPath;
                        break;
                    }
                }

                if (foundFile != null)
                {
                    var json = File.ReadAllText(foundFile);
                    _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    LoggingService.Instance.Info($"Localization loaded for culture '{cultureName}' from: {foundFile}");
                }
                else
                {
                    _strings = new Dictionary<string, string>();
                    LoggingService.Instance.Warning($"Localization file not found for culture '{cultureName}'");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to load localization resources", ex);
                _strings = new Dictionary<string, string>();
            }
        }

        public void SetLanguage(string languageCode)
        {
            try
            {
                var newCulture = CultureInfo.GetCultureInfo(languageCode);
                _currentCulture = newCulture;
                LoadResources();
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to set language to '{languageCode}'", ex);
            }
        }

        public string GetString(string key)
        {
            try
            {
                if (_strings != null && _strings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
                return key;
            }
            catch
            {
                return key;
            }
        }

        public IEnumerable<CultureInfo> GetAvailableLanguages()
        {
            return new List<CultureInfo>
            {
                CultureInfo.GetCultureInfo("uk-UA"),
                CultureInfo.GetCultureInfo("en-US"),
                CultureInfo.GetCultureInfo("ru-RU")
            };
        }
    }
}