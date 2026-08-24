using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ReduxInstaller.Services
{
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAsset[]? Assets { get; set; }
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string? CurrentVersion { get; set; }
        public string? LatestVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ReleasePageUrl { get; set; }
        public string? AssetName { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class UpdateService
    {
        private static UpdateService? _instance;
        public static UpdateService Instance => _instance ??= new UpdateService();

        private readonly HttpClient _httpClient;

        // GitHub repo: markovka320ua/Redux-Installer
        private const string GitHubApiUrl = "https://api.github.com/repos/markovka320ua/Redux-Installer/releases/latest";

        private UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("ReduxInstaller", GetCurrentVersion()));
        }

        public string GetCurrentVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.11";
            }
            catch
            {
                return "1.0.11";
            }
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = GetCurrentVersion()
            };

            try
            {
                LoggingService.Instance.Info("Checking for updates from GitHub...");

                var json = await _httpClient.GetStringAsync(GitHubApiUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    result.ErrorMessage = "Не вдалося отримати інформацію про оновлення.";
                    return result;
                }

                // Normalize version strings (remove 'v' prefix)
                var latestVersionStr = release.TagName.TrimStart('v', 'V');
                result.LatestVersion = latestVersionStr;
                result.ReleasePageUrl = release.HtmlUrl;

                // Compare versions
                if (Version.TryParse(latestVersionStr, out var latestVersion) &&
                    Version.TryParse(result.CurrentVersion, out var currentVersion))
                {
                    result.HasUpdate = latestVersion > currentVersion;
                }
                else
                {
                    // Fallback: compare strings
                    result.HasUpdate = !string.Equals(latestVersionStr, result.CurrentVersion,
                        StringComparison.OrdinalIgnoreCase);
                }

                // Find the executable/zip asset
                if (release.Assets != null && result.HasUpdate)
                {
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name != null && (
                            asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                            asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                        {
                            result.DownloadUrl = asset.BrowserDownloadUrl;
                            result.AssetName = asset.Name;
                            break;
                        }
                    }
                }

                LoggingService.Instance.Info(
                    $"Update check complete. Current: {result.CurrentVersion}, Latest: {result.LatestVersion}, HasUpdate: {result.HasUpdate}");
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = "Немає доступу до мережі або GitHub недоступний.";
                LoggingService.Instance.Warning("Update check failed - network error", ex);
            }
            catch (TaskCanceledException)
            {
                result.ErrorMessage = "Перевірка оновлень перевищила ліміт часу.";
                LoggingService.Instance.Warning("Update check timed out");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "Помилка при перевірці оновлень.";
                LoggingService.Instance.Error("Update check failed", ex);
            }

            return result;
        }

        /// <summary>
        /// Downloads the new version and launches the installer/new exe, then closes current app.
        /// </summary>
        public async Task<bool> DownloadAndApplyUpdateAsync(
            string downloadUrl,
            string assetName,
            IProgress<double>? progress = null)
        {
            try
            {
                LoggingService.Instance.Info($"Downloading update: {assetName}");

                var tempDir = Path.Combine(Path.GetTempPath(), "ReduxInstallerUpdate");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                var tempFilePath = Path.Combine(tempDir, assetName);

                // Download with progress
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                long downloadedBytes = 0;

                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true);

                var buffer = new byte[65536];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        progress?.Report((double)downloadedBytes / totalBytes * 100);
                    }
                }

                LoggingService.Instance.Info($"Update downloaded to: {tempFilePath}");

                // Launch the update
                ApplyUpdate(tempFilePath, assetName);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to download update", ex);
                return false;
            }
        }

        private void ApplyUpdate(string filePath, string assetName)
        {
            try
            {
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                var currentDir = Path.GetDirectoryName(currentExePath) ?? AppDomain.CurrentDomain.BaseDirectory;

                if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    // For EXE: create a batch script that waits for current process to close,
                    // replaces the EXE, and then launches the new one.
                    var batchPath = Path.Combine(Path.GetTempPath(), "redux_update.bat");
                    var newExePath = Path.Combine(currentDir, assetName);

                    var batchContent = $@"@echo off
timeout /t 2 /nobreak > nul
copy /Y ""{filePath}"" ""{newExePath}"" > nul
start """" ""{newExePath}""
del ""%~f0""
";
                    File.WriteAllText(batchPath, batchContent);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchPath,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                }
                else if (assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // For ZIP: open the folder containing the downloaded zip
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                }

                // Shutdown the current application
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to apply update", ex);
            }
        }
    }
}
