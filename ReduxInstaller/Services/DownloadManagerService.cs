using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ReduxInstaller.Models;

namespace ReduxInstaller.Services
{
    public class DownloadManagerService
    {
        private static DownloadManagerService? _instance;
        private static readonly object _lock = new object();
        private readonly HttpClient _httpClient;
        private DateTime _lastSpeedUpdate;
        private long _lastBytesDownloaded;

        public static DownloadManagerService Instance => _instance ??= new DownloadManagerService();

        public ObservableCollection<DownloadTask> ActiveDownloads { get; } = new ObservableCollection<DownloadTask>();
        public event EventHandler<DownloadTask>? DownloadTaskAdded;
        public event EventHandler<DownloadTask>? DownloadTaskUpdated;
        public event EventHandler<DownloadTask>? DownloadTaskCompleted;
        public event EventHandler<DownloadTask>? DownloadTaskFailed;

        private DownloadManagerService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(30);

            try
            {
                BindingOperations.EnableCollectionSynchronization(ActiveDownloads, _lock);
            }
            catch
            {
                // Ignore if not on UI thread yet
            }
        }

        public DownloadTask CreateDownloadTask(string url, string destinationPath)
        {
            var task = new DownloadTask
            {
                Url = url,
                DestinationPath = destinationPath,
                FileName = Path.GetFileName(destinationPath),
                Status = ActiveDownloadStatus.Idle,
                StartTime = DateTime.Now,
                CancellationTokenSource = new CancellationTokenSource()
            };

            RunOnUIThread(() =>
            {
                lock (_lock)
                {
                    ActiveDownloads.Add(task);
                }
                DownloadTaskAdded?.Invoke(this, task);
            });

            return task;
        }

        public async Task<DownloadTask> StartDownloadAsync(DownloadTask task)
        {
            try
            {
                RunOnUIThread(() =>
                {
                    task.Status = ActiveDownloadStatus.Downloading;
                    task.StartTime = DateTime.Now;
                });

                _lastSpeedUpdate = DateTime.Now;
                _lastBytesDownloaded = 0;

                LoggingService.Instance.Info($"Starting download from: {SanitizeUrl(task.Url)}");

                var destinationDirectory = Path.GetDirectoryName(task.DestinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                var token = task.CancellationTokenSource?.Token ?? CancellationToken.None;

                using var response = await _httpClient.GetAsync(task.Url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                var contentHeaders = response.Content.Headers;
                var totalBytes = contentHeaders?.ContentLength;
                if (totalBytes is not null && totalBytes.Value > 0)
                {
                    RunOnUIThread(() => { task.TotalBytes = totalBytes.Value; });
                    LoggingService.Instance.Info($"Download started. Total size: {FormatBytes(totalBytes.Value)}");
                }
                else
                {
                    LoggingService.Instance.Info("Download started. Total size: unknown");
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(token);
                using var fileStream = new FileStream(task.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true);

                var buffer = new byte[65536];
                var bytesRead = 0;
                var totalBytesRead = 0L;
                var lastUiUpdate = DateTime.Now;

                while ((bytesRead = await contentStream.ReadAsync(buffer, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    totalBytesRead += bytesRead;

                    var now = DateTime.Now;
                    if ((now - lastUiUpdate).TotalMilliseconds >= 120)
                    {
                        lastUiUpdate = now;
                        var currentTotal = totalBytesRead;
                        var currentProgress = totalBytes.HasValue && totalBytes.Value > 0 ? (double)currentTotal / totalBytes.Value * 100 : 0;
                        var currentSpeed = CalculateDownloadSpeed(currentTotal);
                        var currentTimeRemaining = CalculateTimeRemaining(currentTotal, totalBytes, currentSpeed);

                        RunOnUIThread(() =>
                        {
                            task.BytesDownloaded = currentTotal;
                            task.ProgressPercentage = currentProgress;
                            task.DownloadSpeed = currentSpeed;
                            task.TimeRemaining = currentTimeRemaining;
                            DownloadTaskUpdated?.Invoke(this, task);
                        });
                    }
                }

                await fileStream.FlushAsync(token);

                RunOnUIThread(() =>
                {
                    task.BytesDownloaded = totalBytesRead;
                    task.ProgressPercentage = 100;
                    task.Status = ActiveDownloadStatus.Completed;
                    task.EndTime = DateTime.Now;
                    DownloadTaskCompleted?.Invoke(this, task);
                });

                LoggingService.Instance.Info($"Download completed: {task.DestinationPath}");
                return task;
            }
            catch (OperationCanceledException)
            {
                RunOnUIThread(() =>
                {
                    task.Status = ActiveDownloadStatus.Cancelled;
                    task.EndTime = DateTime.Now;
                    DownloadTaskFailed?.Invoke(this, task);
                });

                LoggingService.Instance.Info("Download cancelled");

                if (File.Exists(task.DestinationPath))
                {
                    try
                    {
                        File.Delete(task.DestinationPath);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Warning($"Failed to delete partial download file: {task.DestinationPath}", ex);
                    }
                }

                return task;
            }
            catch (Exception ex)
            {
                RunOnUIThread(() =>
                {
                    task.Status = ActiveDownloadStatus.Failed;
                    task.ErrorMessage = ex.Message;
                    task.EndTime = DateTime.Now;
                    DownloadTaskFailed?.Invoke(this, task);
                });

                LoggingService.Instance.Error("Download failed", ex);
                return task;
            }
        }

        public void CancelDownload(DownloadTask task)
        {
            if (task.CancellationTokenSource != null && !task.CancellationTokenSource.IsCancellationRequested)
            {
                LoggingService.Instance.Info($"Cancelling download: {task.FileName}");
                task.CancellationTokenSource.Cancel();
            }
        }

        public void RemoveDownload(DownloadTask task)
        {
            RunOnUIThread(() =>
            {
                lock (_lock)
                {
                    ActiveDownloads.Remove(task);
                }
            });
        }

        private void RunOnUIThread(Action action)
        {
            var app = Application.Current;
            if (app != null && app.Dispatcher != null)
            {
                if (app.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    app.Dispatcher.InvokeAsync(action, System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            else
            {
                action();
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

        private double CalculateDownloadSpeed(long totalBytesRead)
        {
            var now = DateTime.Now;
            var timeElapsed = (now - _lastSpeedUpdate).TotalSeconds;

            if (timeElapsed >= 0.5)
            {
                var bytesSinceLastUpdate = totalBytesRead - _lastBytesDownloaded;
                var speed = bytesSinceLastUpdate / timeElapsed;

                _lastBytesDownloaded = totalBytesRead;
                _lastSpeedUpdate = now;

                return speed;
            }

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
    }
}
