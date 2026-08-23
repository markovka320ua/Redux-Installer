using System;
using System.Windows;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class NotificationDialog : Window
    {
        private bool _isConfirmDialog;

        public NotificationDialog(string title, string message, NotificationType type, bool isConfirmDialog = false)
        {
            try
            {
                InitializeComponent();
                _isConfirmDialog = isConfirmDialog;
                
                TitleTextBlock.Text = title;
                MessageTextBlock.Text = message;
                
                SetupNotificationType(type);
                SetupButtons();
                
                // Add animation
                this.Loaded += (s, e) =>
                {
                    try
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 0.9,
                            To = 1.0,
                            Duration = System.TimeSpan.FromMilliseconds(200),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                        };
                        
                        this.RenderTransform = new System.Windows.Media.ScaleTransform(0.9, 0.9, 0.5, 0.5);
                        this.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
                        this.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Error("Error in animation", ex);
                    }
                };
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error in NotificationDialog constructor", ex);
            }
        }

        private void SetupNotificationType(NotificationType type)
        {
            try
            {
                switch (type)
                {
                    case NotificationType.Success:
                        IconTextBlock.Text = "✓";
                        IconTextBlock.Foreground = (System.Windows.Media.Brush)Resources["SuccessBrush"];
                        break;
                    case NotificationType.Error:
                        IconTextBlock.Text = "✕";
                        IconTextBlock.Foreground = (System.Windows.Media.Brush)Resources["ErrorBrush"];
                        break;
                    case NotificationType.Warning:
                        IconTextBlock.Text = "⚠";
                        IconTextBlock.Foreground = (System.Windows.Media.Brush)Resources["WarningBrush"];
                        break;
                    case NotificationType.Info:
                        IconTextBlock.Text = "ℹ";
                        IconTextBlock.Foreground = (System.Windows.Media.Brush)Resources["InfoBrush"];
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error in SetupNotificationType", ex);
            }
        }

        private void SetupButtons()
        {
            try
            {
                if (_isConfirmDialog)
                {
                    PrimaryButton.Content = LocalizationService.Instance.GetString("notification_confirm");
                    SecondaryButton.Visibility = Visibility.Visible;
                    SecondaryButton.Content = LocalizationService.Instance.GetString("notification_cancel");
                }
                else
                {
                    PrimaryButton.Content = LocalizationService.Instance.GetString("CommonOK");
                    SecondaryButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error in SetupButtons", ex);
                // Fallback to default text
                if (_isConfirmDialog)
                {
                    PrimaryButton.Content = "Confirm";
                    SecondaryButton.Visibility = Visibility.Visible;
                    SecondaryButton.Content = "Cancel";
                }
                else
                {
                    PrimaryButton.Content = "OK";
                    SecondaryButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConfirmDialog)
                {
                    DialogResult = true;
                }
                else
                {
                    DialogResult = true;
                }
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error in PrimaryButton_Click", ex);
                Close();
            }
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error in SecondaryButton_Click", ex);
                Close();
            }
        }
    }
}