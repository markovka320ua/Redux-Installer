using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ReduxInstaller.Models;

namespace ReduxInstaller.Services
{
    public class ReduxCatalogService
    {
        private static ReduxCatalogService? _instance;
        public static ReduxCatalogService Instance => _instance ??= new ReduxCatalogService();

        private readonly HttpClient _httpClient;
        private List<ReduxModItem> _cachedMods = new List<ReduxModItem>();

        // Remote GitHub Raw URL for mods catalog
        private const string CatalogUrl = "https://raw.githubusercontent.com/markovka320ua/Redux-Installer/main/redux_mods.json";

        private ReduxCatalogService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ReduxInstaller");
        }

        public async Task<List<ReduxModItem>> GetModsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedMods.Count > 0)
            {
                return _cachedMods;
            }

            try
            {
                LoggingService.Instance.Info("Fetching Redux mods catalog from GitHub...");

                // Add cache buster to always get the latest content from GitHub raw
                var urlWithCacheBust = $"{CatalogUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                var json = await _httpClient.GetStringAsync(urlWithCacheBust);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var mods = JsonSerializer.Deserialize<List<ReduxModItem>>(json, options);
                if (mods != null && mods.Count > 0)
                {
                    _cachedMods = mods;
                    SaveLocalCache(json);
                    LoggingService.Instance.Info($"Loaded {mods.Count} Redux mods from GitHub");
                    return _cachedMods;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning("Failed to fetch mods from GitHub, trying local cache/fallback", ex);
            }

            // Fallback to local cached file or built-in file
            var localMods = LoadLocalCache();
            if (localMods != null && localMods.Count > 0)
            {
                _cachedMods = localMods;
                return _cachedMods;
            }

            return new List<ReduxModItem>();
        }

        private void SaveLocalCache(string json)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cacheDir = Path.Combine(appData, "ReduxInstaller", "Cache");
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                File.WriteAllText(Path.Combine(cacheDir, "redux_mods.json"), json);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning("Failed to save local catalog cache", ex);
            }
        }

        private List<ReduxModItem>? LoadLocalCache()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cacheFile = Path.Combine(appData, "ReduxInstaller", "Cache", "redux_mods.json");
                if (File.Exists(cacheFile))
                {
                    var json = File.ReadAllText(cacheFile);
                    return JsonSerializer.Deserialize<List<ReduxModItem>>(json);
                }

                // Or check application directory
                var baseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "redux_mods.json");
                if (File.Exists(baseFile))
                {
                    var json = File.ReadAllText(baseFile);
                    return JsonSerializer.Deserialize<List<ReduxModItem>>(json);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning("Failed to read local catalog cache", ex);
            }

            return null;
        }
    }
}
