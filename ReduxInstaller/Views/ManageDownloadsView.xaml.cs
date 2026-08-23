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

        private void ManageDownloadsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var downloadManager = DownloadManagerService.Instance;
            downloadManager.DownloadTaskAdded += DownloadManager_DownloadTaskAdded;
            downloadManager.DownloadTaskUpdated += DownloadManager_DownloadTaskUpdated;
            downloadManager.DownloadTaskCompleted += DownloadManager_DownloadTaskCompleted;
            downloadManager.DownloadTaskFailed += DownloadManager_DownloadTaskFailed;

            LoadActiveDownloads();
        }

        private void ManageDownloadsView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
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
                ActiveDownloadsListBox.ItemsSource = downloadManager.ActiveDownloads;

                if (downloadManager.ActiveDownloads.Count == 0)
                {
                    ActiveDownloadsListBox.ItemsSource = null;
                    var emptyText = new TextBlock
                    {
                        Text = LocalizationService.Instance.GetString("download_empty"),
                        FontSize = 14,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new System.Windows.Thickness(0, 40, 0, 0)
                    };
                    ActiveDownloadsListBox.Items.Add(emptyText);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to load active downloads", ex);
                ActiveDownloadsListBox.ItemsSource = null;
                var errorText = new TextBlock
                {
                    Text = "Не вдалося завантажити активні завантаження",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Red,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new System.Windows.Thickness(0, 40, 0, 0)
                };
                ActiveDownloadsListBox.Items.Add(errorText);
            }
        }

        private void DownloadManager_DownloadTaskAdded(object? sender, DownloadTask task)
        {
            LoadActiveDownloads();
        }

        private void DownloadManager_DownloadTaskUpdated(object? sender, DownloadTask task)
        {
            // ListBox will auto-update due to ObservableCollection
        }

        private void DownloadManager_DownloadTaskCompleted(object? sender, DownloadTask task)
        {
            LoadActiveDownloads();
        }

        private void DownloadManager_DownloadTaskFailed(object? sender, DownloadTask task)
        {
            LoadActiveDownloads();
        }

        private void PauseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Implement pause functionality
            NotificationService.Instance.ShowInfo(
                LocalizationService.Instance.GetString("download_pause"),
                "Пауза завантаження ще не реалізована");
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is DownloadTask task)
            {
                DownloadManagerService.Instance.CancelDownload(task);
            }
        }

        private void RetryButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is DownloadTask task)
            {
                NotificationService.Instance.ShowInfo(
                    LocalizationService.Instance.GetString("download_retry"),
                    $"Повтор завантаження {task.FileName} ще не реалізовано");
            }
        }

        private void ClearHistory_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NotificationService.Instance.ShowConfirm(
                LocalizationService.Instance.GetString("download_clear_history"),
                "Ви впевнені, що хочете очистити всі активні завантаження?",
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
                    NotificationService.Instance.ShowSuccess(
                        LocalizationService.Instance.GetString("download_clear_history"),
                        "Активні завантаження успішно очищені");
                }
            );
        }
    }
}
