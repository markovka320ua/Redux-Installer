using System.Windows;
using Microsoft.Win32;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class GtaVSelectionDialog : Window
    {
        public string? SelectedPath { get; private set; }

        public GtaVSelectionDialog()
        {
            InitializeComponent();
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Пошук GTA V...";
            StatusText.Foreground = (System.Windows.Media.Brush)Resources["MutedTextBrush"];

            var gtaVService = new GtaVService();
            var detectedPath = gtaVService.AutoDetectGtaV();

            if (!string.IsNullOrEmpty(detectedPath))
            {
                SelectedPath = detectedPath;
                StatusText.Text = $"Знайдено: {detectedPath}";
                StatusText.Foreground = (System.Windows.Media.Brush)Resources["SuccessBrush"];
                
                // Auto-close after successful detection
                System.Threading.Thread.Sleep(1000);
                DialogResult = true;
                Close();
            }
            else
            {
                StatusText.Text = "GTA V не знайдено в типових місцях. Спробуйте вибрати папку вручну.";
                StatusText.Foreground = (System.Windows.Media.Brush)Resources["WarningBrush"];
                NotificationService.Instance.ShowWarning(
                    LocalizationService.Instance.GetString("GtaVSelectionTitle"),
                    "GTA V не знайдено в типових місцях. Спробуйте вибрати папку вручну.");
            }
        }

        private void ManualSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = LocalizationService.Instance.GetString("GtaVSelectionTitle")
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FolderName;
                var gtaVService = new GtaVService();

                if (gtaVService.IsValidGtaVInstallation(selectedPath))
                {
                    SelectedPath = selectedPath;
                    StatusText.Text = $"Вибрано: {selectedPath}";
                    StatusText.Foreground = (System.Windows.Media.Brush)Resources["SuccessBrush"];
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    StatusText.Text = "Вибрана папка не містить GTA V. Спробуйте іншу папку.";
                    StatusText.Foreground = (System.Windows.Media.Brush)Resources["ErrorBrush"];
                    NotificationService.Instance.ShowError(
                        LocalizationService.Instance.GetString("GtaVSelectionTitle"),
                        "Вибрана папка не містить GTA V. Спробуйте іншу папку.");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}