using System.Windows.Controls;
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

        private void HomeView_Loaded(object sender, System.Windows.RoutedEventArgs e)
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
                }
                else
                {
                    GtaVStatusText.Text = LocalizationService.Instance.GetString("HomeGtaVPathNotSet");
                    GtaVPathText.Text = "";
                }
            }
            else
            {
                GtaVStatusText.Text = LocalizationService.Instance.GetString("HomeGtaVPathNotSet");
                GtaVPathText.Text = "";
            }
        }

        private void InstallButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Navigate to Install view
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateToInstall();
            }
        }
    }
}