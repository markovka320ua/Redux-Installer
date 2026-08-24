using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ReduxInstaller.Services;
using ReduxInstaller.Models;

namespace ReduxInstaller.Views
{
    public partial class InstallView : UserControl
    {
        private readonly DispatcherTimer _speedUpdateTimer;
        private DownloadTask? _currentDownloadTask;
        private string? _downloadedFilePath;
        private bool _isInstalling;
        private List<ReduxModItem> _allMods = new List<ReduxModItem>();
        private ReduxModItem? _selectedMod;

        public bool IsInstalling => _isInstalling;

        public InstallView()
        {
            InitializeComponent();
            _speedUpdateTimer = new DispatcherTimer();
            _speedUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            _speedUpdateTimer.Tick += SpeedUpdateTimer_Tick;

            Loaded += InstallView_Loaded;
            Unloaded += InstallView_Unloaded;
        }

        private async void InstallView_Loaded(object sender, RoutedEventArgs e)
        {
            // Subscribe to download manager events
            var downloadManager = DownloadManagerService.Instance;
            downloadManager.DownloadTaskUpdated += DownloadManager_DownloadTaskUpdated;
            downloadManager.DownloadTaskCompleted += DownloadManager_DownloadTaskCompleted;
            downloadManager.DownloadTaskFailed += DownloadManager_DownloadTaskFailed;

            // Check if there's an active download
            if (downloadManager.ActiveDownloads.Count > 0)
            {
                var activeTask = downloadManager.ActiveDownloads[0];
                if (activeTask.Status == ActiveDownloadStatus.Downloading || activeTask.Status == ActiveDownloadStatus.Extracting)
                {
                    _currentDownloadTask = activeTask;
                    _isInstalling = true;
                    _downloadedFilePath = activeTask.DestinationPath;
                    ProgressModName.Text = activeTask.FileName;
                    ProgressCard.Visibility = Visibility.Visible;
                    InstallButton.IsEnabled = false;

                    // Show download manager button
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    mainWindow?.ShowDownloadManagerButton();

                    UpdateProgressUI(activeTask);
                }
                else
                {
                    ResetUI();
                }
            }
            else
            {
                ResetUI();
            }

            // Load catalog of mods from GitHub
            await LoadCatalogAsync();
        }

        private void InstallView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from events when navigating away
            var downloadManager = DownloadManagerService.Instance;
            downloadManager.DownloadTaskUpdated -= DownloadManager_DownloadTaskUpdated;
            downloadManager.DownloadTaskCompleted -= DownloadManager_DownloadTaskCompleted;
            downloadManager.DownloadTaskFailed -= DownloadManager_DownloadTaskFailed;
        }

        private async System.Threading.Tasks.Task LoadCatalogAsync()
        {
            try
            {
                CatalogLoadingBorder.Visibility = Visibility.Visible;
                CatalogEmptyBorder.Visibility = Visibility.Collapsed;

                _allMods = await ReduxCatalogService.Instance.GetModsAsync();

                CatalogLoadingBorder.Visibility = Visibility.Collapsed;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                CatalogLoadingBorder.Visibility = Visibility.Collapsed;
                LoggingService.Instance.Error("Failed to load catalog", ex);
            }
        }

        private void ApplyFilter()
        {
            var query = SearchTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allMods
                : _allMods.Where(m =>
                    m.Title.ToLowerInvariant().Contains(query) ||
                    m.ShortDescription.ToLowerInvariant().Contains(query) ||
                    m.FullDescription.ToLowerInvariant().Contains(query) ||
                    (m.Badge != null && m.Badge.ToLowerInvariant().Contains(query))).ToList();

            ModsItemsControl.ItemsSource = filtered;

            if (filtered.Count == 0 && _allMods.Count > 0)
            {
                CatalogEmptyBorder.Visibility = Visibility.Visible;
            }
            else
            {
                CatalogEmptyBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        #region Tab Switching (Catalog / Custom URL)

        private void CatalogTabBtn_Click(object sender, RoutedEventArgs e)
        {
            SwitchToCatalogTab();
        }

        private void CustomUrlTabBtn_Click(object sender, RoutedEventArgs e)
        {
            SwitchToCustomUrlTab();
        }

        private void SwitchToCatalogTab()
        {
            CatalogTabBtn.Style = (Style)FindResource("ActiveTabPillButton");
            CustomUrlTabBtn.Style = (Style)FindResource("TabPillButton");
            CatalogViewContainer.Visibility = Visibility.Visible;
            CustomUrlViewContainer.Visibility = Visibility.Collapsed;
            SearchBoxBorder.Visibility = Visibility.Visible;
        }

        private void SwitchToCustomUrlTab()
        {
            CatalogTabBtn.Style = (Style)FindResource("TabPillButton");
            CustomUrlTabBtn.Style = (Style)FindResource("ActiveTabPillButton");
            CatalogViewContainer.Visibility = Visibility.Collapsed;
            CustomUrlViewContainer.Visibility = Visibility.Visible;
            SearchBoxBorder.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Mod Card Details Modal Dialog

        private void ModCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ReduxModItem mod)
            {
                OpenModDetailsModal(mod);
            }
        }

        private void OpenModDetailsModal(ReduxModItem mod)
        {
            _selectedMod = mod;

            // Fill details
            ModalModTitle.Text = mod.Title;
            ModalModDescription.Text = string.IsNullOrWhiteSpace(mod.FullDescription) ? mod.ShortDescription : mod.FullDescription;
            ModalModVersion.Text = $"Версія: {mod.Version}";

            if (mod.HasBadge)
            {
                ModalModBadge.Visibility = Visibility.Visible;
                ModalModBadgeText.Text = mod.Badge;
            }
            else
            {
                ModalModBadge.Visibility = Visibility.Collapsed;
            }

            if (mod.HasSize)
            {
                ModalModSizeBorder.Visibility = Visibility.Visible;
                ModalModSize.Text = $"Розмір: {mod.Size}";
            }
            else
            {
                ModalModSizeBorder.Visibility = Visibility.Collapsed;
            }

            if (mod.HasAuthor)
            {
                ModalModAuthorBorder.Visibility = Visibility.Visible;
                ModalModAuthor.Text = $"Автор: {mod.Author}";
            }
            else
            {
                ModalModAuthorBorder.Visibility = Visibility.Collapsed;
            }

            // Image
            try
            {
                if (!string.IsNullOrWhiteSpace(mod.ImageUrl))
                {
                    ModalModImage.Source = new BitmapImage(new Uri(mod.ImageUrl, UriKind.RelativeOrAbsolute));
                }
                else
                {
                    ModalModImage.Source = null;
                }
            }
            catch
            {
                ModalModImage.Source = null;
            }

            // Video button
            ModalWatchVideoBtn.Visibility = mod.HasVideo ? Visibility.Visible : Visibility.Collapsed;

            // Show Modal
            ModDetailsModal.Visibility = Visibility.Visible;
        }

        private void CloseModal_Click(object sender, RoutedEventArgs e)
        {
            ModDetailsModal.Visibility = Visibility.Collapsed;
            _selectedMod = null;
        }

        private void WatchVideoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMod != null && _selectedMod.HasVideo)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _selectedMod.VideoUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Error("Failed to open video url", ex);
                }
            }
        }

        private void ModalInstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMod == null) return;

            var downloadUrl = _selectedMod.DownloadUrl;
            var modTitle = _selectedMod.Title;

            ModDetailsModal.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                ShowError("Для цього моду ще не вказано пряме посилання на завантаження.");
                return;
            }

            StartDownloadProcess(downloadUrl, modTitle);
        }

        #endregion

        #region Installation Logic

        private void ResetUI()
        {
            ProgressCard.Visibility = Visibility.Collapsed;
            SuccessCard.Visibility = Visibility.Collapsed;
            UrlTextBox.Text = string.Empty;
            _isInstalling = false;
            _currentDownloadTask = null;
            _downloadedFilePath = null;

            // Hide download manager button when installation is complete
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.HideDownloadManagerButton();
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlTextBox.Text?.Trim() ?? string.Empty;

            // Validate URL
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowError(LocalizationService.Instance.GetString("ErrorUrlEmpty"));
                return;
            }

            if (!IsValidUrl(url))
            {
                ShowError(LocalizationService.Instance.GetString("ErrorUrlInvalid"));
                return;
            }

            StartDownloadProcess(url, "Custom Redux");
        }

        private async void StartDownloadProcess(string url, string modDisplayName)
        {
            // Check GTA V path
            var settingsService = SettingsService.Instance;
            if (!settingsService.IsGtaVPathSet())
            {
                ShowError(LocalizationService.Instance.GetString("ErrorGtaVNotFound"));
                return;
            }

            var gtaVPath = settingsService.GetGtaVPath();
            var gtaVService = new GtaVService();
            if (gtaVPath == null || !gtaVService.IsValidGtaVInstallation(gtaVPath))
            {
                ShowError(LocalizationService.Instance.GetString("ErrorGtaVNotFound"));
                return;
            }

            // Start download
            _isInstalling = true;
            ProgressModName.Text = modDisplayName;
            ProgressCard.Visibility = Visibility.Visible;
            SuccessCard.Visibility = Visibility.Collapsed;
            InstallButton.IsEnabled = false;

            // Show download manager button
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ShowDownloadManagerButton();

            try
            {
                var downloadManager = DownloadManagerService.Instance;
                var tempFilePath = downloadManager.GetTempFilePath();
                _downloadedFilePath = tempFilePath;

                // Create download task
                _currentDownloadTask = downloadManager.CreateDownloadTask(url, tempFilePath);

                // Start download
                await downloadManager.StartDownloadAsync(_currentDownloadTask);

                // Download completed, start extraction
                await StartExtraction(_downloadedFilePath, gtaVPath);
            }
            catch (OperationCanceledException)
            {
                // Download was cancelled
                ResetUI();
                ShowMessage(LocalizationService.Instance.GetString("DownloadCancelled"));
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Installation failed", ex);
                ResetUI();
                ShowError(LocalizationService.Instance.GetString("ErrorDownloadFailed"));
            }
            finally
            {
                InstallButton.IsEnabled = true;
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private void DownloadManager_DownloadTaskUpdated(object? sender, DownloadTask task)
        {
            if (_currentDownloadTask != null && task.Id == _currentDownloadTask.Id)
            {
                Dispatcher.Invoke(() => UpdateProgressUI(task));
            }
        }

        private void DownloadManager_DownloadTaskCompleted(object? sender, DownloadTask task)
        {
            if (_currentDownloadTask != null && task.Id == _currentDownloadTask.Id)
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressTitle.Text = LocalizationService.Instance.GetString("DownloadComplete");
                });
            }
        }

        private void DownloadManager_DownloadTaskFailed(object? sender, DownloadTask task)
        {
            if (_currentDownloadTask != null && task.Id == _currentDownloadTask.Id)
            {
                Dispatcher.Invoke(() =>
                {
                    ResetUI();
                    ShowError(LocalizationService.Instance.GetString("ErrorDownloadFailed"));
                });
            }
        }

        private void UpdateProgressUI(DownloadTask task)
        {
            ProgressBar.Value = task.ProgressPercentage;
            ProgressText.Text = $"{task.DownloadedSize} / {task.TotalSize}";

            if (task.DownloadSpeed > 0)
            {
                SpeedText.Text = $"{LocalizationService.Instance.GetString("DownloadSpeed")}: {task.SpeedText}";
            }

            if (task.TimeRemaining.HasValue)
            {
                TimeText.Text = $"{LocalizationService.Instance.GetString("DownloadTimeRemaining")}: {task.TimeRemainingText}";
            }
        }

        private async System.Threading.Tasks.Task StartExtraction(string zipPath, string destinationPath)
        {
            try
            {
                ProgressTitle.Text = LocalizationService.Instance.GetString("InstallPreparing");

                var zipService = ZipService.Instance;

                // Validate ZIP
                if (!zipService.IsValidZipFile(zipPath))
                {
                    throw new InvalidOperationException(LocalizationService.Instance.GetString("ErrorZipCorrupted"));
                }

                // Check disk space
                var zipSize = zipService.GetZipSize(zipPath);
                var estimatedSize = zipService.GetEstimatedExtractedSize(zipPath);
                var diskSpaceService = DiskSpaceService.Instance;

                if (!diskSpaceService.HasEnoughSpaceForDownloadAndExtraction(destinationPath, zipSize, estimatedSize))
                {
                    throw new InvalidOperationException(LocalizationService.Instance.GetString("ErrorDiskSpace"));
                }

                // Check for existing files
                var hasConflicts = zipService.CheckForExistingFiles(zipPath, destinationPath, out var conflictingFiles);
                if (hasConflicts)
                {
                    NotificationService.Instance.ShowConfirm(
                        LocalizationService.Instance.GetString("ErrorFilesExist"),
                        LocalizationService.Instance.GetString("ErrorFilesExistDescription"),
                        onConfirm: () => { },
                        onCancel: () => { ResetUI(); }
                    );
                    return;
                }

                // Start extraction
                ProgressTitle.Text = LocalizationService.Instance.GetString("InstallProgress");
                zipService.ExtractionProgressChanged += ZipService_ExtractionProgressChanged;
                zipService.ExtractionCompleted += ZipService_ExtractionCompleted;
                zipService.ExtractionFailed += ZipService_ExtractionFailed;

                await zipService.ExtractZipAsync(zipPath, destinationPath);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Extraction failed", ex);
                ResetUI();
                ShowError(ex.Message);
            }
        }

        private void ZipService_ExtractionProgressChanged(object? sender, ExtractionProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = e.ProgressPercentage;
                ProgressText.Text = $"{e.FilesExtracted} / {e.TotalFiles} {LocalizationService.Instance.GetString("Downloaded")}";
            });
        }

        private void ZipService_ExtractionCompleted(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Clean up downloaded file
                if (!string.IsNullOrEmpty(_downloadedFilePath) && File.Exists(_downloadedFilePath))
                {
                    try
                    {
                        File.Delete(_downloadedFilePath);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Warning("Failed to delete downloaded file", ex);
                    }
                }

                // Remove download task from active downloads
                if (_currentDownloadTask != null)
                {
                    DownloadManagerService.Instance.RemoveDownload(_currentDownloadTask);
                }

                // Show success
                ProgressCard.Visibility = Visibility.Collapsed;
                SuccessCard.Visibility = Visibility.Visible;
                _isInstalling = false;
            });
        }

        private void ZipService_ExtractionFailed(object? sender, Exception e)
        {
            Dispatcher.Invoke(() =>
            {
                ResetUI();
                ShowError(LocalizationService.Instance.GetString("ErrorExtractionFailed"));
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling && _currentDownloadTask != null)
            {
                DownloadManagerService.Instance.CancelDownload(_currentDownloadTask);
                ZipService.Instance.CancelExtraction();
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.NavigateToHome();
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = SettingsService.Instance;
            var gtaVPath = settingsService.GetGtaVPath();

            if (!string.IsNullOrEmpty(gtaVPath) && Directory.Exists(gtaVPath))
            {
                try
                {
                    Process.Start("explorer.exe", gtaVPath);
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Error("Failed to open GTA V folder", ex);
                }
            }
        }

        private void SpeedUpdateTimer_Tick(object? sender, EventArgs e)
        {
            // Timer for updating speed display
        }

        private void ShowError(string message)
        {
            NotificationService.Instance.ShowError(LocalizationService.Instance.GetString("InstallTitle"), message);
        }

        private void ShowMessage(string message)
        {
            NotificationService.Instance.ShowInfo(LocalizationService.Instance.GetString("InstallTitle"), message);
        }

        #endregion
    }
}