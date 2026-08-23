using System;
using System.IO;
using System.Threading;

namespace ReduxInstaller.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LoggingService
    {
        private static LoggingService? _instance;
        private static readonly object _lock = new object();
        private readonly string _logDirectory;

        public static LoggingService Instance => _instance ??= new LoggingService();

        private LoggingService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "ReduxInstaller", "Logs");
            
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch
            {
                // If we can't create log directory, we'll log to temp
                _logDirectory = Path.Combine(Path.GetTempPath(), "ReduxInstaller", "Logs");
                try
                {
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }
                }
                catch
                {
                    // Last resort - use temp directory directly
                    _logDirectory = Path.GetTempPath();
                }
            }

            CleanOldLogs();
        }

        private void CleanOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(_logDirectory, "*.log");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < DateTime.Now.AddDays(-7))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore deletion errors
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            try
            {
                var logFileName = $"ReduxInstaller_{DateTime.Now:yyyyMMdd}.log";
                var logFilePath = Path.Combine(_logDirectory, logFileName);

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var levelText = level.ToString().ToUpper();
                var exceptionText = exception != null ? $"\nException: {exception.Message}\nStackTrace: {exception.StackTrace}" : "";

                var logEntry = $"[{timestamp}] [{levelText}] {message}{exceptionText}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(logFilePath, logEntry);
                }
            }
            catch
            {
                // If logging fails, we don't want to crash the application
            }
        }

        public void Debug(string message, Exception? exception = null) => Log(LogLevel.Debug, message, exception);
        public void Info(string message, Exception? exception = null) => Log(LogLevel.Info, message, exception);
        public void Warning(string message, Exception? exception = null) => Log(LogLevel.Warning, message, exception);
        public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);

        public string GetLogDirectory()
        {
            return _logDirectory;
        }

        public void OpenLogDirectory()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _logDirectory);
            }
            catch (Exception ex)
            {
                Error("Failed to open log directory", ex);
            }
        }
    }
}