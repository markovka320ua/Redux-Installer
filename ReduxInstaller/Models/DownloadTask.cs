using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ReduxInstaller.Models
{
    public class DownloadTask : INotifyPropertyChanged
    {
        private string _url = string.Empty;
        private string _fileName = string.Empty;
        private string _destinationPath = string.Empty;
        private ActiveDownloadStatus _status = ActiveDownloadStatus.Idle;
        private long _bytesDownloaded;
        private long _totalBytes;
        private double _progressPercentage;
        private double _downloadSpeed;
        private TimeSpan? _timeRemaining;
        private DateTime _startTime;
        private DateTime? _endTime;
        private string? _errorMessage;
        private CancellationTokenSource? _cancellationTokenSource;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }
        
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }
        
        public string DestinationPath
        {
            get => _destinationPath;
            set { _destinationPath = value; OnPropertyChanged(); }
        }
        
        public ActiveDownloadStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); OnPropertyChanged(nameof(StatusText)); }
        }
        
        public long BytesDownloaded
        {
            get => _bytesDownloaded;
            set { _bytesDownloaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadedSize)); }
        }
        
        public long TotalBytes
        {
            get => _totalBytes;
            set { _totalBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalSize)); }
        }
        
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set { _progressPercentage = value; OnPropertyChanged(); }
        }
        
        public double DownloadSpeed
        {
            get => _downloadSpeed;
            set { _downloadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedText)); }
        }
        
        public TimeSpan? TimeRemaining
        {
            get => _timeRemaining;
            set { _timeRemaining = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeRemainingText)); }
        }
        
        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }
        
        public DateTime? EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(); }
        }
        
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }
        
        public CancellationTokenSource? CancellationTokenSource
        {
            get => _cancellationTokenSource;
            set { _cancellationTokenSource = value; OnPropertyChanged(); }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
