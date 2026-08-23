using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReduxInstaller.Models;

namespace ReduxInstaller.Services
{
    public class DownloadHistoryService
    {
        private static DownloadHistoryService? _instance;
        private static readonly object _lock = new object();
        private readonly string _historyFilePath;
        private List<DownloadHistoryItem> _history;
        private const int MaxHistoryItems = 100;

        public static DownloadHistoryService Instance => _instance ??= new DownloadHistoryService();

        public List<DownloadHistoryItem> History => _history;

        public event EventHandler? HistoryChanged;

        private DownloadHistoryService()
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
                    configDirectory = Path.GetTempPath();
                }
            }

            _historyFilePath = Path.Combine(configDirectory, "downloads.json");
            _history = LoadHistory();
        }

        private List<DownloadHistoryItem> LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    var history = JsonSerializer.Deserialize<List<DownloadHistoryItem>>(json);
                    if (history != null)
                    {
                        LoggingService.Instance.Info("Download history loaded successfully");
                        return history;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to load download history", ex);
            }

            return new List<DownloadHistoryItem>();
        }

        public void SaveHistory()
        {
            try
            {
                var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFilePath, json);
                LoggingService.Instance.Info("Download history saved successfully");
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to save download history", ex);
            }
        }

        public void AddDownload(string fileName, string url, long fileSize, string destinationPath)
        {
            var item = new DownloadHistoryItem
            {
                FileName = fileName,
                Url = url,
                DateTime = DateTime.Now,
                Status = DownloadStatus.Completed,
                FileSize = fileSize,
                DestinationPath = destinationPath
            };

            _history.Insert(0, item);

            // Keep only the last 100 items
            if (_history.Count > MaxHistoryItems)
            {
                _history = _history.Take(MaxHistoryItems).ToList();
            }

            SaveHistory();
        }

        public void UpdateDownloadStatus(string id, DownloadStatus status)
        {
            var item = _history.FirstOrDefault(h => h.Id == id);
            if (item != null)
            {
                item.Status = status;
                SaveHistory();
            }
        }

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        public DownloadHistoryItem? GetDownloadById(string id)
        {
            return _history.FirstOrDefault(h => h.Id == id);
        }
    }
}
