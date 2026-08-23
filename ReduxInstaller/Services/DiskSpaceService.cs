using System;
using System.IO;

namespace ReduxInstaller.Services
{
    public class DiskSpaceService
    {
        private static DiskSpaceService? _instance;

        public static DiskSpaceService Instance => _instance ??= new DiskSpaceService();

        private DiskSpaceService()
        {
        }

        public long GetAvailableDiskSpace(string path)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
                return driveInfo.AvailableFreeSpace;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error getting available disk space for {path}", ex);
                return 0;
            }
        }

        public long GetTotalDiskSpace(string path)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
                return driveInfo.TotalSize;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error getting total disk space for {path}", ex);
                return 0;
            }
        }

        public bool HasEnoughSpace(string path, long requiredBytes)
        {
            try
            {
                var availableSpace = GetAvailableDiskSpace(path);
                var hasEnough = availableSpace >= requiredBytes;

                if (!hasEnough)
                {
                    Services.LoggingService.Instance.Warning($"Insufficient disk space. Required: {FormatBytes(requiredBytes)}, Available: {FormatBytes(availableSpace)}");
                }
                else
                {
                    Services.LoggingService.Instance.Info($"Sufficient disk space. Required: {FormatBytes(requiredBytes)}, Available: {FormatBytes(availableSpace)}");
                }

                return hasEnough;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error checking disk space for {path}", ex);
                return false;
            }
        }

        public bool HasEnoughSpaceForDownloadAndExtraction(string path, long zipSize, long estimatedExtractedSize)
        {
            try
            {
                // We need space for:
                // 1. The downloaded ZIP file (in temp directory)
                // 2. The extracted files (in GTA V directory)
                // 3. Some buffer for safety
                
                var totalRequired = zipSize + estimatedExtractedSize + (100 * 1024 * 1024); // 100MB buffer
                var availableSpace = GetAvailableDiskSpace(path);
                
                // Also check temp directory space (usually same drive as system)
                var tempPath = Path.GetTempPath();
                var tempDriveSpace = GetAvailableDiskSpace(tempPath);
                
                var hasEnoughForExtraction = availableSpace >= estimatedExtractedSize + (100 * 1024 * 1024);
                var hasEnoughForDownload = tempDriveSpace >= zipSize + (100 * 1024 * 1024);

                var result = hasEnoughForExtraction && hasEnoughForDownload;

                if (!result)
                {
                    LoggingService.Instance.Warning($"Insufficient disk space. Extraction space: {FormatBytes(availableSpace)}, Download space: {FormatBytes(tempDriveSpace)}, Required: {FormatBytes(totalRequired)}");
                }
                else
                {
                    LoggingService.Instance.Info($"Sufficient disk space for download and extraction");
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error checking disk space for download and extraction", ex);
                return false;
            }
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

        public string GetDriveInfo(string path)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
                var available = FormatBytes(driveInfo.AvailableFreeSpace);
                var total = FormatBytes(driveInfo.TotalSize);
                var usedPercent = ((driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (double)driveInfo.TotalSize * 100);

                return $"{driveInfo.Name} - {available} вільно з {total} ({usedPercent:0.0}% використано)";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error getting drive info for {path}", ex);
                return "Невідомо";
            }
        }
    }
}