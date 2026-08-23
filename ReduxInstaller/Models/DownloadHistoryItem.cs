using System;

namespace ReduxInstaller.Models
{
    public class DownloadHistoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public DownloadStatus Status { get; set; }
        public long FileSize { get; set; }
        public string DestinationPath { get; set; } = string.Empty;
    }

    public enum DownloadStatus
    {
        Completed,
        Failed,
        Cancelled
    }
}