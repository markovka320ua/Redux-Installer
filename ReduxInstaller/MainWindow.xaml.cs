using System.Windows;
using ReduxInstaller.Services;
using ReduxInstaller.Views;

namespace ReduxInstaller;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to Home view by default
        NavigateToHome();
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
        var installView = ContentArea.Content as InstallView;
        if (installView != null && installView.IsInstalling)
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

    private void SetActiveNavButton(System.Windows.Controls.Button activeButton)
    {
        // Reset all nav buttons to default style
        HomeNavButton.Style = (System.Windows.Style)Resources["NavButton"];
        InstallNavButton.Style = (System.Windows.Style)Resources["NavButton"];
        SettingsNavButton.Style = (System.Windows.Style)Resources["NavButton"];
        AboutNavButton.Style = (System.Windows.Style)Resources["NavButton"];

        // Set active button style
        activeButton.Style = (System.Windows.Style)Resources["ActiveNavButton"];
    }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            this.DragMove();
    }
}