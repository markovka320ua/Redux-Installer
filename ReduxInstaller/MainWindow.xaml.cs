using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ReduxInstaller.Services;
using ReduxInstaller.Views;

namespace ReduxInstaller;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly Geometry MaximizeGeometry = Geometry.Parse("M 0 0 L 10 0 L 10 10 L 0 10 Z");
    private static readonly Geometry RestoreGeometry = Geometry.Parse("M 2 0 L 10 0 L 10 8 L 8 8 L 8 10 L 0 10 L 0 2 L 2 2 Z M 2 2 L 2 8 L 8 8 L 8 2 Z");

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
        this.StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to Home view by default
        NavigateToHome();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            MaximizeIconPath.Data = RestoreGeometry;
            MaximizeBtn.ToolTip = "Restore";
        }
        else
        {
            MaximizeIconPath.Data = MaximizeGeometry;
            MaximizeBtn.ToolTip = "Maximize";
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CheckAndClose();
    }

    private void CheckAndClose()
    {
        // Check if there's an active installation
        if (ContentArea.Content is InstallView installView && installView.IsInstalling)
        {
            NotificationService.Instance.ShowConfirm(
                LocalizationService.Instance.GetString("WindowCloseConfirmTitle"),
                LocalizationService.Instance.GetString("WindowCloseConfirmMessage"),
                onConfirm: () => { this.Close(); }
            );
            return;
        }

        this.Close();
    }

    private void HomeNavButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToHome();
    }

    private void InstallNavButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToInstall();
    }

    private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToSettings();
    }

    private void AboutNavButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToAbout();
    }

    private void DownloadManagerNavButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToDownloadManager();
    }

    public void NavigateToHome()
    {
        SetActiveNavButton(HomeNavButton);
        ContentArea.Content = new HomeView();
    }

    public void NavigateToInstall()
    {
        SetActiveNavButton(InstallNavButton);
        ContentArea.Content = new InstallView();
    }

    public void NavigateToSettings()
    {
        SetActiveNavButton(SettingsNavButton);
        ContentArea.Content = new SettingsView();
    }

    public void NavigateToAbout()
    {
        SetActiveNavButton(AboutNavButton);
        ContentArea.Content = new AboutView();
    }

    public void NavigateToDownloadManager()
    {
        SetActiveNavButton(DownloadManagerNavButton);
        ContentArea.Content = new ManageDownloadsView();
    }

    public void ShowDownloadManagerButton()
    {
        DownloadManagerNavButton.Visibility = Visibility.Visible;
    }

    public void HideDownloadManagerButton()
    {
        DownloadManagerNavButton.Visibility = Visibility.Collapsed;
    }

    private void SetActiveNavButton(Button activeButton)
    {
        var navStyle = (Style)FindResource("NavButton");
        var activeStyle = (Style)FindResource("ActiveNavButton");

        HomeNavButton.Style = navStyle;
        InstallNavButton.Style = navStyle;
        SettingsNavButton.Style = navStyle;
        AboutNavButton.Style = navStyle;
        DownloadManagerNavButton.Style = navStyle;

        activeButton.Style = activeStyle;
    }
}