using System;
using System.Windows;
using ReduxInstaller.Views;

namespace ReduxInstaller.Services
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class NotificationService
    {
        private static NotificationService? _instance;

        public static NotificationService Instance => _instance ??= new NotificationService();

        private NotificationService()
        {
        }

        public void Show(string title, string message, NotificationType type)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var notification = new NotificationDialog(title, message, type);
                    notification.Owner = Application.Current.MainWindow;
                    notification.ShowDialog();
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to show notification", ex);
                // Fallback to simple message box if notification dialog fails
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ShowSuccess(string title, string message)
        {
            Show(title, message, NotificationType.Success);
        }

        public void ShowError(string title, string message)
        {
            Show(title, message, NotificationType.Error);
        }

        public void ShowWarning(string title, string message)
        {
            Show(title, message, NotificationType.Warning);
        }

        public void ShowInfo(string title, string message)
        {
            Show(title, message, NotificationType.Info);
        }

        public void ShowConfirm(string title, string message, Action onConfirm, Action? onCancel = null)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var notification = new NotificationDialog(title, message, NotificationType.Warning, true);
                    notification.Owner = Application.Current.MainWindow;
                    var result = notification.ShowDialog();
                    
                    if (result == true)
                    {
                        onConfirm?.Invoke();
                    }
                    else
                    {
                        onCancel?.Invoke();
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to show confirmation dialog", ex);
                // Fallback to simple message box if notification dialog fails
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    onConfirm?.Invoke();
                }
                else
                {
                    onCancel?.Invoke();
                }
            }
        }
    }
}