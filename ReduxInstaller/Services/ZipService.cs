using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReduxInstaller.Services
{
    public class ExtractionProgressEventArgs : EventArgs
    {
        public int FilesExtracted { get; set; }
        public int TotalFiles { get; set; }
        public double ProgressPercentage { get; set; }
        public string? CurrentFile { get; set; }
    }

    public class ZipService
    {
        private static ZipService? _instance;
        private CancellationTokenSource? _cancellationTokenSource;

        public static ZipService Instance => _instance ??= new ZipService();

        public event EventHandler<ExtractionProgressEventArgs>? ExtractionProgressChanged;
        public event EventHandler? ExtractionCompleted;
        public event EventHandler<Exception>? ExtractionFailed;

        public bool IsExtracting { get; private set; }

        private ZipService()
        {
        }

        private string GetCommonPrefix(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return "";

            if (paths.Length == 1)
                return Path.GetDirectoryName(paths[0]) ?? "";

            var firstPath = paths[0];
            var commonPrefix = new StringBuilder();
            
            for (int i = 0; i < firstPath.Length; i++)
            {
                char currentChar = firstPath[i];
                bool allMatch = true;
                
                foreach (var path in paths)
                {
                    if (i >= path.Length || path[i] != currentChar)
                    {
                        allMatch = false;
                        break;
                    }
                }
                
                if (!allMatch)
                    break;
                
                commonPrefix.Append(currentChar);
            }

            // Get the last directory from the common prefix
            var commonPrefixStr = commonPrefix.ToString();
            var lastSlash = commonPrefixStr.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                return commonPrefixStr.Substring(0, lastSlash);
            }

            return "";
        }

        private bool ShouldStripPrefix(string commonPrefix, string[] entries)
        {
            if (string.IsNullOrEmpty(commonPrefix))
                return false;

            // Check if all entries start with the common prefix
            return entries.All(entry => entry.StartsWith(commonPrefix, StringComparison.OrdinalIgnoreCase));
        }

        private string StripPrefix(string path, string prefix)
        {
            if (string.IsNullOrEmpty(prefix) || !path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return path;

            var remainingPath = path.Substring(prefix.Length);
            
            // Remove leading slash or backslash
            if (remainingPath.StartsWith("/") || remainingPath.StartsWith("\\"))
            {
                remainingPath = remainingPath.Substring(1);
            }

            return remainingPath;
        }

        public bool IsValidZipFile(string zipPath)
        {
            try
            {
                LoggingService.Instance.Info($"Validating ZIP file: {zipPath}");

                if (!File.Exists(zipPath))
                {
                    LoggingService.Instance.Error($"ZIP file does not exist: {zipPath}");
                    return false;
                }

                // Try to open the zip file to validate it
                using (var zipArchive = ZipFile.OpenRead(zipPath))
                {
                    // Check if the archive has any entries
                    if (!zipArchive.Entries.Any())
                    {
                        LoggingService.Instance.Warning("ZIP file is empty");
                        return false;
                    }

                    // Try to read the first entry to ensure it's not corrupted
                    var firstEntry = zipArchive.Entries.First();
                    using (var stream = firstEntry.Open())
                    {
                        // Just try to read a byte to verify the entry is valid
                        var buffer = new byte[1];
                        stream.Read(buffer, 0, 1);
                    }
                }

                LoggingService.Instance.Info("ZIP file is valid");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"ZIP file validation failed: {zipPath}", ex);
                return false;
            }
        }

        public long GetZipSize(string zipPath)
        {
            try
            {
                return new FileInfo(zipPath).Length;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to get ZIP file size: {zipPath}", ex);
                return 0;
            }
        }

        public long GetEstimatedExtractedSize(string zipPath)
        {
            try
            {
                using var zipArchive = ZipFile.OpenRead(zipPath);
                return zipArchive.Entries.Sum(entry => entry.Length);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to estimate extracted size: {zipPath}", ex);
                return 0;
            }
        }

        public int GetFileCount(string zipPath)
        {
            try
            {
                using var zipArchive = ZipFile.OpenRead(zipPath);
                return zipArchive.Entries.Count;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to get file count: {zipPath}", ex);
                return 0;
            }
        }

        public async Task ExtractZipAsync(string zipPath, string destinationPath, CancellationToken cancellationToken = default)
        {
            try
            {
                IsExtracting = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                LoggingService.Instance.Info($"Starting extraction to: {destinationPath}");

                // Ensure destination directory exists
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                using var zipArchive = ZipFile.OpenRead(zipPath);
                var totalFiles = zipArchive.Entries.Count;
                var filesExtracted = 0;

                // Get all entry paths for common prefix detection
                var entryPaths = zipArchive.Entries
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Select(e => e.FullName)
                    .ToArray();

                // Find common prefix
                var commonPrefix = GetCommonPrefix(entryPaths);
                var shouldStripPrefix = ShouldStripPrefix(commonPrefix, entryPaths);

                if (shouldStripPrefix)
                {
                    LoggingService.Instance.Info($"Stripping common prefix: {commonPrefix}");
                }

                foreach (var entry in zipArchive.Entries)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Skip directory entries
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    // Determine the destination path
                    var entryPath = entry.FullName;
                    if (shouldStripPrefix)
                    {
                        entryPath = StripPrefix(entryPath, commonPrefix);
                    }

                    // Validate the entry path to prevent Zip Slip
                    if (!IsPathSafe(entryPath, destinationPath))
                    {
                        throw new InvalidOperationException($"Potentially malicious path detected in ZIP: {entryPath}");
                    }

                    var destinationEntryPath = Path.Combine(destinationPath, entryPath);
                    
                    // Ensure the directory exists
                    var destinationDirectory = Path.GetDirectoryName(destinationEntryPath);
                    if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    // Extract the file
                    await Task.Run(() =>
                    {
                        entry.ExtractToFile(destinationEntryPath, overwrite: true);
                    }, _cancellationTokenSource.Token);

                    filesExtracted++;

                    // Update progress
                    var progress = totalFiles > 0 ? (double)filesExtracted / totalFiles * 100 : 0;
                    var progressArgs = new ExtractionProgressEventArgs
                    {
                        FilesExtracted = filesExtracted,
                        TotalFiles = totalFiles,
                        ProgressPercentage = progress,
                        CurrentFile = entryPath
                    };

                    ExtractionProgressChanged?.Invoke(this, progressArgs);
                }

                IsExtracting = false;
                LoggingService.Instance.Info($"Extraction completed successfully. {filesExtracted} files extracted.");
                ExtractionCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                IsExtracting = false;
                LoggingService.Instance.Info("Extraction cancelled");
                throw;
            }
            catch (Exception ex)
            {
                IsExtracting = false;
                LoggingService.Instance.Error("Extraction failed", ex);
                ExtractionFailed?.Invoke(this, ex);
                throw;
            }
        }

        private bool IsPathSafe(string entryPath, string destinationPath)
        {
            try
            {
                // Normalize the paths
                var normalizedEntryPath = Path.GetFullPath(Path.Combine(destinationPath, entryPath));
                var normalizedDestinationPath = Path.GetFullPath(destinationPath);

                // Check if the entry path starts with the destination path
                return normalizedEntryPath.StartsWith(normalizedDestinationPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public void CancelExtraction()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                LoggingService.Instance.Info("Cancelling extraction");
                _cancellationTokenSource.Cancel();
            }
        }

        public bool CheckForExistingFiles(string zipPath, string destinationPath, out string[] conflictingFiles)
        {
            try
            {
                LoggingService.Instance.Info("Checking for existing files in destination");

                using var zipArchive = ZipFile.OpenRead(zipPath);
                var conflicts = zipArchive.Entries
                    .Where(entry => !string.IsNullOrEmpty(entry.Name))
                    .Select(entry => Path.Combine(destinationPath, entry.FullName))
                    .Where(File.Exists)
                    .ToArray();

                conflictingFiles = conflicts;
                
                if (conflicts.Length > 0)
                {
                    LoggingService.Instance.Info($"Found {conflicts.Length} conflicting files");
                }
                else
                {
                    LoggingService.Instance.Info("No conflicting files found");
                }

                return conflicts.Length > 0;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to check for existing files", ex);
                conflictingFiles = Array.Empty<string>();
                return false;
            }
        }
    }
}