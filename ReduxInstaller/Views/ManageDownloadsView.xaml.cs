using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ReduxInstaller.Services;
using ReduxInstaller.Models;

namespace ReduxInstaller.Views
{
    public partial class ManageDownloadsView : UserControl
    {
        public ManageDownloadsView()
        {
            InitializeComponent();
            Loaded += ManageDownloadsView_Loaded;
            Unloaded += ManageDownloadsView_Unloaded;
        }

        private void ManageDownloadsView_Loaded(object sender, RoutedEventArgs e)
        {
            var downloadManager = DownloadManagerService.Instance;
            downloadManager.DownloadTaskAdded += DownloadManager_DownloadTaskAdded;
            downloadManager.DownloadTaskUpdated += DownloadManager_DownloadTaskUpdated;
            downloadManager.DownloadTaskCompleted += DownloadManager_DownloadTaskCompleted;
            downloadManager.DownloadTaskFailed += DownloadManager_DownloadTaskFailed;

            LoadActiveDownloads();
        }

        private void ManageDownloadsView_Unloaded(object sender, RoutedEventArgs e)
        {
            var downloadManager = DownloadManagerService.Instance;
            downloadManager.DownloadTaskAdded -= DownloadManager_DownloadTaskAdded;
            downloadManager.DownloadTaskUpdated -= DownloadManager_DownloadTaskUpdated;
            downloadManager.DownloadTaskCompleted -= DownloadManager_DownloadTaskCompleted;
            downloadManager.DownloadTaskFailed -= DownloadManager_DownloadTaskFailed;
        }

        private void LoadActiveDownloads()
        {
            try
            {
                var downloadManager = DownloadManagerService.Instance;
                var downloads = downloadManager.ActiveDownloads;
                
                ActiveDownloadsListBox.ItemsSource = downloads;

                if (downloads == null || downloads.Count == 0)
                {
                    EmptyStateBorder.Visibility = Visibility.Visible;
                    ActiveDownloadsListBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyStateBorder.Visibility = Visibility.Collapsed;
                    ActiveDownloadsListBox.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to load active downloads", ex);
            }
        }

        private void DownloadManager_DownloadTaskAdded(object? sender, DownloadTask task)
        {
            Dispatcher.Invoke(LoadActiveDownloads);
        }

        private void DownloadManager_DownloadTaskUpdated(object? sender, DownloadTask task)
        {
            // ObservableCollection auto-updates
        }

        private void DownloadManager_DownloadTaskCompleted(object? sender, DownloadTask task)
        {
            Dispatcher.Invoke(LoadActiveDownloads);
        }

        private void DownloadManager_DownloadTaskFailed(object? sender, DownloadTask task)
        {
            Dispatcher.Invoke(LoadActiveDownloads);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is DownloadTask task)
            {
                DownloadManagerService.Instance.CancelDownload(task);
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is DownloadTask task)
            {
                NotificationService.Instance.ShowInfo(
                    LocalizationService.Instance.GetString("download_retry"),
                    $"{task.FileName}");
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            NotificationService.Instance.ShowConfirm(
                LocalizationService.Instance.GetString("download_clear_history"),
                LocalizationService.Instance.GetString("download_clear_history") + "?",
                onConfirm: () =>
                {
                    var downloadManager = DownloadManagerService.Instance;
                    foreach (var task in downloadManager.ActiveDownloads.ToList())
                    {
                        if (task.Status == ActiveDownloadStatus.Completed || task.Status == ActiveDownloadStatus.Failed || task.Status == ActiveDownloadStatus.Cancelled)
                        {
                            downloadManager.RemoveDownload(task);
                        }
                    }
                    LoadActiveDownloads();
                }
            );
        }
    }
}
