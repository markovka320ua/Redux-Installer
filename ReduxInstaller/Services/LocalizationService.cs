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

        public string CurrentLanguage => _currentCulture.TwoLetterISOLanguageName;

        private LocalizationService()
        {
            _currentCulture = CultureInfo.GetCultureInfo("uk-UA");
            LoadResources();
        }

        private void LoadResources()
        {
            try
            {
                var resourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Localization", _currentCulture.Name, "strings.json");
                
                if (File.Exists(resourceFile))
                {
                    var json = File.ReadAllText(resourceFile);
                    _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                else
                {
                    _strings = new Dictionary<string, string>();
                }
            }
            catch
            {
                _strings = new Dictionary<string, string>();
            }
        }

        public void SetLanguage(string languageCode)
        {
            try
            {
                var newCulture = CultureInfo.GetCultureInfo(languageCode);
                if (_currentCulture.Name != newCulture.Name)
                {
                    _currentCulture = newCulture;
                    LoadResources();
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                // Keep current language if change fails
            }
        }

        public string GetString(string key)
        {
            try
            {
                if (_strings != null && _strings.TryGetValue(key, out var value))
                {
                    return value;
                }
                return key; // Return key if not found
            }
            catch
            {
                return key;
            }
        }

        public IEnumerable<CultureInfo> GetAvailableLanguages()
        {
            var languages = new List<CultureInfo>
            {
                CultureInfo.GetCultureInfo("uk-UA")
                // Add more languages here in the future
            };
            return languages;
        }
    }
}