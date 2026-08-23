using System.Windows.Controls;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class ManageDownloadsView : UserControl
    {
        public ManageDownloadsView()
        {
            InitializeComponent();
            Loaded += ManageDownloadsView_Loaded;
        }

        private void ManageDownloadsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadHistory();
        }

        private void LoadHistory()
        {
            var historyService = DownloadHistoryService.Instance;
            HistoryListBox.ItemsSource = historyService.History;

            if (historyService.History.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("download_empty"),
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new System.Windows.Thickness(0, 40, 0, 0)
                };
                HistoryListBox.Items.Add(emptyText);
            }
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
            // TODO: Implement cancel functionality
            NotificationService.Instance.ShowInfo(
                LocalizationService.Instance.GetString("download_cancel"),
                "Скасування завантаження ще не реалізовано");
        }

        private void RetryButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Implement retry functionality
            var button = sender as Button;
            if (button?.DataContext is Models.DownloadHistoryItem item)
            {
                NotificationService.Instance.ShowInfo(
                    LocalizationService.Instance.GetString("download_retry"),
                    $"Повтор завантаження {item.FileName} ще не реалізовано");
            }
        }

        private void ClearHistory_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NotificationService.Instance.ShowConfirm(
                LocalizationService.Instance.GetString("download_clear_history"),
                "Ви впевнені, що хочете очистити всю історію завантажень?",
                onConfirm: () =>
                {
                    DownloadHistoryService.Instance.ClearHistory();
                    LoadHistory();
                    NotificationService.Instance.ShowSuccess(
                        LocalizationService.Instance.GetString("download_clear_history"),
                        "Історія успішно очищена");
                }
            );
        }
    }
}
