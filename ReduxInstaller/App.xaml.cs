using System.Windows;
using ReduxInstaller.Services;
using ReduxInstaller.Views;

namespace ReduxInstaller;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        LoggingService.Instance.Info("Application starting");

        var settingsService = SettingsService.Instance;
        var savedLanguage = settingsService.GetLanguage();
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            LocalizationService.Instance.SetLanguage(savedLanguage);
        }
        
        // Check if GTA V path is configured
        if (!settingsService.IsGtaVPathSet())
        {
            LoggingService.Instance.Info("GTA V path not configured, showing selection dialog");
            
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Show GTA V selection dialog after main window loads
            mainWindow.Loaded += (s, args) =>
            {
                var dialog = new GtaVSelectionDialog
                {
                    Owner = mainWindow
                };

                if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    settingsService.SetGtaVPath(dialog.SelectedPath);
                    LoggingService.Instance.Info($"GTA V path configured: {dialog.SelectedPath}");
                }
                else
                {
                    LoggingService.Instance.Warning("GTA V path configuration cancelled by user");
                }
            };
        }
        else
        {
            LoggingService.Instance.Info("GTA V path already configured");
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggingService.Instance.Info("Application shutting down");
        base.OnExit(e);
    }
}

