using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class NotificationDialog : Window
    {
        private bool _isConfirmDialog;

        private static readonly Geometry SuccessGeometry = Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z");
        private static readonly Geometry ErrorGeometry = Geometry.Parse("M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z");
        private static readonly Geometry WarningGeometry = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");
        private static readonly Geometry InfoGeometry = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");

        public NotificationDialog(string title, string message, NotificationType type, bool isConfirmDialog = false)
        {
            try
            {
                InitializeComponent();
                _isConfirmDialog = isConfirmDialog;
                
                TitleTextBlock.Text = title;
                MessageTextBlock.Text = message;
                MessageTextBlock.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
                
                SetupNotificationType(type);
                SetupButtons();
                
                // Subtle pop-in animation
                this.Loaded += (s, e) =>
                {
                    try
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 0.94,
                            To = 1.0,
                            Duration = TimeSpan.FromMilliseconds(180),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                        };
                        
                        this.RenderTransform = new ScaleTransform(0.94, 0.94, 0.5, 0.5);
                        this.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                        this.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
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

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void SetupNotificationType(NotificationType type)
        {
            try
            {
                switch (type)
                {
                    case NotificationType.Success:
                        IconPath.Data = SuccessGeometry;
                        IconPath.Fill = (Brush)FindResource("SuccessBrush");
                        IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(0x25, 0x10, 0xB9, 0x81));
                        break;
                    case NotificationType.Error:
                        IconPath.Data = ErrorGeometry;
                        IconPath.Fill = (Brush)FindResource("ErrorBrush");
                        IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(0x25, 0xEF, 0x44, 0x44));
                        break;
                    case NotificationType.Warning:
                        IconPath.Data = WarningGeometry;
                        IconPath.Fill = (Brush)FindResource("WarningBrush");
                        IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(0x25, 0xF5, 0x9E, 0x0B));
                        break;
                    case NotificationType.Info:
                        IconPath.Data = InfoGeometry;
                        IconPath.Fill = (Brush)FindResource("InfoBrush");
                        IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(0x25, 0x3B, 0x82, 0xF6));
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
            catch
            {
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
            DialogResult = true;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}