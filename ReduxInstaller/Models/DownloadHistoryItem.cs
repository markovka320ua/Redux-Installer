using System;
using System.Windows;

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

        // Computed properties for UI binding
        public string StatusIcon => Status switch
        {
            DownloadStatus.Completed => "✅",
            DownloadStatus.Failed => "❌",
            DownloadStatus.Cancelled => "⏹",
            _ => "❓"
        };

        public string DateTimeString => DateTime.ToString("dd.MM.yyyy HH:mm");

        public string FileSizeString => FormatFileSize(FileSize);

        public Visibility RetryVisible => Status == DownloadStatus.Failed ? Visibility.Visible : Visibility.Collapsed;

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    public enum DownloadStatus
    {
        Completed,
        Failed,
        Cancelled
    }
}