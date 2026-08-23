using System;
using System.Threading.Tasks;
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

        // Global unhandled exception handlers to prevent crashes and log all errors
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LoggingService.Instance.Error("Unhandled AppDomain Exception", ex);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            LoggingService.Instance.Error("Unhandled Dispatcher Exception", args.Exception);
            args.Handled = true; // Prevents crash
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            LoggingService.Instance.Error("Unobserved Task Exception", args.Exception);
            args.SetObserved();
        };

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
