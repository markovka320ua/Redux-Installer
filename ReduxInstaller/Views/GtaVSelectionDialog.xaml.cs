using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private async void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = LocalizationService.Instance.GetString("InstallPreparing");
            StatusText.Foreground = (Brush)FindResource("MutedTextBrush");

            var gtaVService = new GtaVService();
            var detectedPath = gtaVService.AutoDetectGtaV();

            if (!string.IsNullOrEmpty(detectedPath))
            {
                SelectedPath = detectedPath;
                StatusText.Text = $"{LocalizationService.Instance.GetString("HomeGtaVFound")}: {detectedPath}";
                StatusText.Foreground = (Brush)FindResource("SuccessBrush");
                
                await Task.Delay(600);
                DialogResult = true;
                Close();
            }
            else
            {
                StatusText.Text = LocalizationService.Instance.GetString("ErrorGtaVNotFound");
                StatusText.Foreground = (Brush)FindResource("WarningBrush");
                NotificationService.Instance.ShowWarning(
                    LocalizationService.Instance.GetString("GtaVSelectionTitle"),
                    LocalizationService.Instance.GetString("ErrorGtaVNotFound"));
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
                    StatusText.Text = $"{LocalizationService.Instance.GetString("HomeGtaVFound")}: {selectedPath}";
                    StatusText.Foreground = (Brush)FindResource("SuccessBrush");
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    StatusText.Text = LocalizationService.Instance.GetString("ErrorGtaVNotFound");
                    StatusText.Foreground = (Brush)FindResource("ErrorBrush");
                    NotificationService.Instance.ShowError(
                        LocalizationService.Instance.GetString("GtaVSelectionTitle"),
                        LocalizationService.Instance.GetString("ErrorGtaVNotFound"));
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