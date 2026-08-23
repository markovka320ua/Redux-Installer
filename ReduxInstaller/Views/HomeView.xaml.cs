using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            Loaded += HomeView_Loaded;
        }

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGtaVStatus();
        }

        private void UpdateGtaVStatus()
        {
            var settingsService = SettingsService.Instance;
            var gtaVService = new GtaVService();

            if (settingsService.IsGtaVPathSet())
            {
                var gtaVPath = settingsService.GetGtaVPath();
                if (gtaVPath != null && gtaVService.IsValidGtaVInstallation(gtaVPath))
                {
                    GtaVStatusText.Text = LocalizationService.Instance.GetString("HomeGtaVFound");
                    GtaVPathText.Text = gtaVPath;
                    StatusDot.Fill = (Brush)FindResource("SuccessBrush");
                    return;
                }
            }

            GtaVStatusText.Text = LocalizationService.Instance.GetString("HomeGtaVPathNotSet");
            GtaVPathText.Text = "";
            StatusDot.Fill = (Brush)FindResource("WarningBrush");
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToInstall();
            }
        }

        private void ChangeLocation_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToSettings();
            }
        }
    }
}