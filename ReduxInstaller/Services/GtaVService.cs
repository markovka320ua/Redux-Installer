using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReduxInstaller.Services
{
    public class GtaVService
    {
        private static readonly string[] GtaVIndicatorFiles = new[]
        {
            "GTA5.exe",
            "PlayGTAV.exe",
            "GTAVLauncher.exe",
            "GTA5.exe" // Primary indicator
        };

        private static readonly string[] CommonGtaVPaths = new[]
        {
            @"C:\Program Files\Rockstar Games\Grand Theft Auto V",
            @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V",
            @"C:\Program Files\Epic Games\Grand Theft Auto V",
            @"D:\Games\Grand Theft Auto V",
            @"E:\Games\Grand Theft Auto V",
            @"F:\Games\Grand Theft Auto V"
        };

        public bool IsValidGtaVInstallation(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    return false;
                }

                // Check for primary indicator file
                var gta5Exe = Path.Combine(path, "GTA5.exe");
                if (File.Exists(gta5Exe))
                {
                    LoggingService.Instance.Info($"Valid GTA V installation found at: {path}");
                    return true;
                }

                // Check for secondary indicator files
                var hasIndicatorFile = GtaVIndicatorFiles
                    .Any(file => File.Exists(Path.Combine(path, file)));

                if (hasIndicatorFile)
                {
                    LoggingService.Instance.Info($"Valid GTA V installation found at: {path}");
                    return true;
                }

                LoggingService.Instance.Warning($"Directory exists but is not a valid GTA V installation: {path}");
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error validating GTA V installation at {path}", ex);
                return false;
            }
        }

        public string? AutoDetectGtaV()
        {
            LoggingService.Instance.Info("Starting auto-detection of GTA V");

            foreach (var path in CommonGtaVPaths)
            {
                try
                {
                    if (IsValidGtaVInstallation(path))
                    {
                        LoggingService.Instance.Info($"Auto-detected GTA V at: {path}");
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Debug($"Error checking path {path}", ex);
                }
            }

            // Try to detect from Steam library path
            var steamPath = DetectFromSteam();
            if (steamPath != null)
            {
                LoggingService.Instance.Info($"Auto-detected GTA V from Steam at: {steamPath}");
                return steamPath;
            }

            // Try to detect from Epic Games
            var epicPath = DetectFromEpicGames();
            if (epicPath != null)
            {
                LoggingService.Instance.Info($"Auto-detected GTA V from Epic Games at: {epicPath}");
                return epicPath;
            }

            LoggingService.Instance.Info("GTA V auto-detection failed - no installation found");
            return null;
        }

        private string? DetectFromSteam()
        {
            try
            {
                var steamPaths = new[]
                {
                    @"C:\Program Files (x86)\Steam",
                    @"C:\Program Files\Steam",
                    @"D:\Steam",
                    @"E:\Steam"
                };

                foreach (var steamPath in steamPaths)
                {
                    if (!Directory.Exists(steamPath))
                        continue;

                    var libraryFolders = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(libraryFolders))
                    {
                        // This is a simplified detection - in production you'd parse the VDF file properly
                        var content = File.ReadAllText(libraryFolders);
                        var lines = content.Split('\n');
                        
                        foreach (var line in lines)
                        {
                            if (line.Contains("\"path\""))
                            {
                                var startIndex = line.IndexOf("\"path\"") + 8;
                                var endIndex = line.IndexOf("\"", startIndex);
                                if (endIndex > startIndex)
                                {
                                    var libraryPath = line.Substring(startIndex, endIndex - startIndex).Replace("\\\\", "\\");
                                    var gtaVPath = Path.Combine(libraryPath, "steamapps", "common", "Grand Theft Auto V");
                                    
                                    if (IsValidGtaVInstallation(gtaVPath))
                                    {
                                        return gtaVPath;
                                    }
                                }
                            }
                        }
                    }

                    // Check default Steam location
                    var defaultGtaVPath = Path.Combine(steamPath, "steamapps", "common", "Grand Theft Auto V");
                    if (IsValidGtaVInstallation(defaultGtaVPath))
                    {
                        return defaultGtaVPath;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Debug("Error detecting from Steam", ex);
            }

            return null;
        }

        private string? DetectFromEpicGames()
        {
            try
            {
                var epicPaths = new[]
                {
                    @"C:\Program Files\Epic Games",
                    @"C:\Program Files (x86)\Epic Games",
                    @"D:\Epic Games",
                    @"E:\Epic Games"
                };

                foreach (var epicPath in epicPaths)
                {
                    if (!Directory.Exists(epicPath))
                        continue;

                    var gtaVPath = Path.Combine(epicPath, "Grand Theft Auto V");
                    if (IsValidGtaVInstallation(gtaVPath))
                    {
                        return gtaVPath;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Debug("Error detecting from Epic Games", ex);
            }

            return null;
        }

        public bool HasWriteAccess(string path)
        {
            try
            {
                var testFile = Path.Combine(path, ".redux_write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning($"No write access to {path}", ex);
                return false;
            }
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
    }
}