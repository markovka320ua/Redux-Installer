using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settingsService = SettingsService.Instance;
            
            // Load GTA V path
            var gtaVPath = settingsService.GetGtaVPath();
            if (!string.IsNullOrEmpty(gtaVPath))
            {
                GtaVPathText.Text = gtaVPath;
                GtaVPathText.Foreground = (System.Windows.Media.Brush)Resources["PrimaryTextBrush"];
            }
            else
            {
                GtaVPathText.Text = "Не налаштовано";
                GtaVPathText.Foreground = (System.Windows.Media.Brush)Resources["SecondaryTextBrush"];
            }

            // Load language
            var currentLanguage = settingsService.GetLanguage();
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void ChangeGtaVPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = LocalizationService.Instance.GetString("GtaVSelectionTitle")
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FolderName;
                    var gtaVService = new GtaVService();

                    if (gtaVService.IsValidGtaVInstallation(selectedPath))
                    {
                        var settingsService = SettingsService.Instance;
                        settingsService.SetGtaVPath(selectedPath);
                        LoadSettings();
                        LoggingService.Instance.Info($"GTA V path updated to: {selectedPath}");
                    }
                    else
                    {
                        NotificationService.Instance.ShowWarning(
                            LocalizationService.Instance.GetString("SettingsTitle"),
                            LocalizationService.Instance.GetString("ErrorGtaVNotFound"));
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to change GTA V path", ex);
                MessageBox.Show("Не вдалося змінити шлях до GTA V.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var languageCode = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(languageCode))
                {
                    var settingsService = SettingsService.Instance;
                    settingsService.SetLanguage(languageCode);
                    LoggingService.Instance.Info($"Language changed to: {languageCode}");
                    
                    // Show restart dialog
                    NotificationService.Instance.ShowConfirm(
                        LocalizationService.Instance.GetString("settings_restart_required"),
                        "",
                        onConfirm: () =>
                        {
                            // Restart application
                            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        },
                        onCancel: () =>
                        {
                            // Revert language selection if cancelled
                            LoadSettings();
                        }
                    );
                }
            }
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.Instance.OpenLogDirectory();
        }

        private void CleanTemp_Click(object sender, RoutedEventArgs e)
        {
            NotificationService.Instance.ShowConfirm(
                "Підтвердження",
                "Ви впевнені, що хочете очистити тимчасові файли?",
                onConfirm: () =>
                {
                    try
                    {
                        DownloadService.Instance.CleanTempFiles();
                        NotificationService.Instance.ShowSuccess(
                            LocalizationService.Instance.GetString("SettingsTitle"),
                            "Тимчасові файли успішно очищено.");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Error("Failed to clean temp files", ex);
                        NotificationService.Instance.ShowError(
                            LocalizationService.Instance.GetString("SettingsTitle"),
                            "Не вдалося очистити тимчасові файли.");
                    }
                }
            );
        }
    }
}