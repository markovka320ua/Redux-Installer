using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class AboutView : UserControl
    {
        private string? _updateDownloadUrl;
        private string? _updateAssetName;

        public AboutView()
        {
            InitializeComponent();

            // Show current version
            VersionBadge.Text = $"v{UpdateService.Instance.GetCurrentVersion()}";
        }

        private void DeveloperButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://t.me/markovka320",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback if opening URL fails
            }
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckUpdateButton.IsEnabled = false;
                CheckUpdateButtonText.Text = LocalizationService.Instance.GetString("AboutCheckingUpdates");
                UpdateStatusBorder.Visibility = Visibility.Collapsed;
                DownloadUpdateButton.Visibility = Visibility.Collapsed;

                var result = await UpdateService.Instance.CheckForUpdatesAsync();

                // Show status
                UpdateStatusBorder.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    UpdateStatusDot.Fill = (Brush)FindResource("WarningBrush");
                    UpdateStatusText.Text = result.ErrorMessage;
                }
                else if (result.HasUpdate)
                {
                    UpdateStatusDot.Fill = (Brush)FindResource("SuccessBrush");
                    UpdateStatusText.Text = $"{LocalizationService.Instance.GetString("AboutUpdateAvailable")} v{result.LatestVersion}";

                    _updateDownloadUrl = result.DownloadUrl;
                    _updateAssetName = result.AssetName;

                    // Show download button if we have a direct download link
                    if (!string.IsNullOrEmpty(_updateDownloadUrl))
                    {
                        DownloadUpdateButton.Visibility = Visibility.Visible;
                    }
                    else if (!string.IsNullOrEmpty(result.ReleasePageUrl))
                    {
                        // Fallback: open release page
                        _updateDownloadUrl = result.ReleasePageUrl;
                        _updateAssetName = null;
                        DownloadUpdateButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    UpdateStatusDot.Fill = (Brush)FindResource("SuccessBrush");
                    UpdateStatusText.Text = $"{LocalizationService.Instance.GetString("AboutUpToDate")} (v{result.CurrentVersion})";
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("CheckUpdate click failed", ex);
                UpdateStatusBorder.Visibility = Visibility.Visible;
                UpdateStatusDot.Fill = (Brush)FindResource("WarningBrush");
                UpdateStatusText.Text = LocalizationService.Instance.GetString("AboutUpdateError");
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
                CheckUpdateButtonText.Text = LocalizationService.Instance.GetString("AboutCheckUpdates");
            }
        }

        private async void DownloadUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_updateDownloadUrl))
                return;

            // If no direct asset URL (only release page), just open browser
            if (string.IsNullOrEmpty(_updateAssetName))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _updateDownloadUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
                return;
            }

            // Direct download update
            try
            {
                DownloadUpdateButton.IsEnabled = false;
                CheckUpdateButton.IsEnabled = false;
                UpdateProgressBorder.Visibility = Visibility.Visible;
                UpdateProgressBar.Value = 0;
                UpdateProgressText.Text = "0%";

                UpdateStatusText.Text = LocalizationService.Instance.GetString("AboutDownloadingUpdate");

                var progress = new Progress<double>(percent =>
                {
                    UpdateProgressBar.Value = percent;
                    UpdateProgressText.Text = $"{percent:F0}%";
                });

                var success = await UpdateService.Instance.DownloadAndApplyUpdateAsync(
                    _updateDownloadUrl,
                    _updateAssetName,
                    progress);

                if (!success)
                {
                    UpdateStatusDot.Fill = (Brush)FindResource("WarningBrush");
                    UpdateStatusText.Text = LocalizationService.Instance.GetString("AboutUpdateError");
                    UpdateProgressBorder.Visibility = Visibility.Collapsed;
                    DownloadUpdateButton.IsEnabled = true;
                    CheckUpdateButton.IsEnabled = true;
                }
                // If success, app will shutdown to apply update
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("DownloadUpdate click failed", ex);
                UpdateStatusDot.Fill = (Brush)FindResource("WarningBrush");
                UpdateStatusText.Text = LocalizationService.Instance.GetString("AboutUpdateError");
                UpdateProgressBorder.Visibility = Visibility.Collapsed;
                DownloadUpdateButton.IsEnabled = true;
                CheckUpdateButton.IsEnabled = true;
            }
        }
    }
}