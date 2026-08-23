using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ReduxInstaller.Services
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double ProgressPercentage { get; set; }
        public double DownloadSpeed { get; set; } // bytes per second
        public TimeSpan? TimeRemaining { get; set; }
    }

    public class DownloadService
    {
        private static DownloadService? _instance;
        private HttpClient? _httpClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private DateTime _downloadStartTime;
        private long _lastBytesDownloaded;
        private DateTime _lastSpeedUpdate;

        public static DownloadService Instance => _instance ??= new DownloadService();

        public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
        public event EventHandler? DownloadCompleted;
        public event EventHandler<Exception>? DownloadFailed;

        public bool IsDownloading { get; private set; }

        private DownloadService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
        }

        public async Task<string> DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            try
            {
                IsDownloading = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _downloadStartTime = DateTime.Now;
                _lastBytesDownloaded = 0;
                _lastSpeedUpdate = DateTime.Now;

                LoggingService.Instance.Info($"Starting download from: {SanitizeUrl(url)}");

                // Ensure destination directory exists
                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                var contentHeaders = response.Content.Headers;
                var totalBytes = contentHeaders?.ContentLength;
                if (totalBytes is not null && totalBytes.Value > 0)
                {
                    LoggingService.Instance.Info($"Download started. Total size: {FormatBytes(totalBytes.Value)}");
                }
                else
                {
                    LoggingService.Instance.Info("Download started. Total size: unknown");
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var bytesRead = 0;
                var totalBytesRead = 0L;

                while ((bytesRead = await contentStream.ReadAsync(buffer, _cancellationTokenSource.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), _cancellationTokenSource.Token);
                    totalBytesRead += bytesRead;

                    // Update progress
                    var progress = totalBytes.HasValue && totalBytes.Value > 0 ? (double)totalBytesRead / totalBytes.Value * 100 : 0;
                    var speed = CalculateDownloadSpeed(totalBytesRead);
                    var timeRemaining = CalculateTimeRemaining(totalBytesRead, totalBytes, speed);

                    var progressArgs = new DownloadProgressEventArgs
                    {
                        BytesDownloaded = totalBytesRead,
                        TotalBytes = totalBytes ?? 0,
                        ProgressPercentage = progress,
                        DownloadSpeed = speed,
                        TimeRemaining = timeRemaining
                    };

                    ProgressChanged?.Invoke(this, progressArgs);
                }

                await fileStream.FlushAsync(_cancellationTokenSource.Token);
                
                IsDownloading = false;
                LoggingService.Instance.Info($"Download completed: {destinationPath}");
                DownloadCompleted?.Invoke(this, EventArgs.Empty);

                return destinationPath;
            }
            catch (OperationCanceledException)
            {
                IsDownloading = false;
                LoggingService.Instance.Info("Download cancelled");
                
                // Clean up partial file
                if (File.Exists(destinationPath))
                {
                    try
                    {
                        File.Delete(destinationPath);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Warning($"Failed to delete partial download file: {destinationPath}", ex);
                    }
                }

                throw;
            }
            catch (Exception ex)
            {
                IsDownloading = false;
                LoggingService.Instance.Error("Download failed", ex);
                DownloadFailed?.Invoke(this, ex);
                throw;
            }
        }

        private double CalculateDownloadSpeed(long totalBytesRead)
        {
            var now = DateTime.Now;
            var timeElapsed = (now - _lastSpeedUpdate).TotalSeconds;

            if (timeElapsed >= 1.0) // Update speed every second
            {
                var bytesSinceLastUpdate = totalBytesRead - _lastBytesDownloaded;
                var speed = bytesSinceLastUpdate / timeElapsed;
                
                _lastBytesDownloaded = totalBytesRead;
                _lastSpeedUpdate = now;
                
                return speed;
            }

            // Return previous speed if less than a second has passed
            return 0;
        }

        private TimeSpan? CalculateTimeRemaining(long totalBytesRead, long? totalBytes, double currentSpeed)
        {
            if (!totalBytes.HasValue || totalBytes.Value == 0 || currentSpeed == 0)
                return null;

            var bytesRemaining = totalBytes.Value - totalBytesRead;
            var secondsRemaining = bytesRemaining / currentSpeed;

            return TimeSpan.FromSeconds(secondsRemaining);
        }

        public void CancelDownload()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                LoggingService.Instance.Info("Cancelling download");
                _cancellationTokenSource.Cancel();
            }
        }

        private string FormatBytes(long? bytes)
        {
            if (!bytes.HasValue)
                return "Unknown";

            return FormatBytes(bytes.Value);
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private string SanitizeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
            }
            catch
            {
                return "invalid_url";
            }
        }

        public string GetTempFilePath()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ReduxInstaller");
            
            try
            {
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to create temp directory", ex);
                tempDir = Path.GetTempPath();
            }

            var fileName = $"redux_{Guid.NewGuid()}.zip";
            return Path.Combine(tempDir, fileName);
        }

        public void CleanTempFiles()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "ReduxInstaller");
                if (Directory.Exists(tempDir))
                {
                    var files = Directory.GetFiles(tempDir, "redux_*.zip");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.Warning($"Failed to delete temp file: {file}", ex);
                        }
                    }
                    LoggingService.Instance.Info($"Cleaned {files.Length} temp files");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to clean temp files", ex);
            }
        }
    }
}