using System;
using System.Threading;

namespace ReduxInstaller.Models
{
    public class DownloadTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public ActiveDownloadStatus Status { get; set; } = ActiveDownloadStatus.Idle;
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double ProgressPercentage { get; set; }
        public double DownloadSpeed { get; set; }
        public TimeSpan? TimeRemaining { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public string StatusIcon => Status switch
        {
            ActiveDownloadStatus.Idle => "⏳",
            ActiveDownloadStatus.Downloading => "⬇️",
            ActiveDownloadStatus.Completed => "✅",
            ActiveDownloadStatus.Failed => "❌",
            ActiveDownloadStatus.Cancelled => "⏹",
            ActiveDownloadStatus.Extracting => "📦",
            _ => "❓"
        };

        public string StatusText => Status switch
        {
            ActiveDownloadStatus.Idle => "Очікування",
            ActiveDownloadStatus.Downloading => "Завантаження",
            ActiveDownloadStatus.Completed => "Завершено",
            ActiveDownloadStatus.Failed => "Помилка",
            ActiveDownloadStatus.Cancelled => "Скасовано",
            ActiveDownloadStatus.Extracting => "Розпакування",
            _ => "Невідомо"
        };

        public string DownloadedSize => FormatBytes(BytesDownloaded);
        public string TotalSize => FormatBytes(TotalBytes);
        public string SpeedText => DownloadSpeed > 0 ? $"{FormatBytes((long)DownloadSpeed)}/s" : "";
        public string TimeRemainingText => TimeRemaining.HasValue ? FormatTime(TimeRemaining.Value) : "";

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

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours > 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            if (time.TotalMinutes > 1)
                return $"{time.Minutes}m {time.Seconds}s";
            return $"{time.Seconds}s";
        }
    }

    public enum ActiveDownloadStatus
    {
        Idle,
        Downloading,
        Completed,
        Failed,
        Cancelled,
        Extracting
    }
}
