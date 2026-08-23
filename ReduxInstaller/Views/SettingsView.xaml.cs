using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using ReduxInstaller.Services;

namespace ReduxInstaller.Views
{
    public partial class SettingsView : UserControl
    {
        private bool _isInitializing = true;

        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            LoadSettings();
            _isInitializing = false;
        }

        private void LoadSettings()
        {
            var settingsService = SettingsService.Instance;
            
            // Load GTA V path
            var gtaVPath = settingsService.GetGtaVPath();
            if (!string.IsNullOrEmpty(gtaVPath))
            {
                GtaVPathText.Text = gtaVPath;
                GtaVPathText.Foreground = (Brush)FindResource("PrimaryTextBrush");
            }
            else
            {
                GtaVPathText.Text = LocalizationService.Instance.GetString("SettingsNotConfigured");
                GtaVPathText.Foreground = (Brush)FindResource("SecondaryTextBrush");
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
                NotificationService.Instance.ShowError(
                    LocalizationService.Instance.GetString("SettingsTitle"),
                    LocalizationService.Instance.GetString("ErrorGtaVNotFound"));
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var languageCode = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(languageCode))
                {
                    var settingsService = SettingsService.Instance;
                    var currentLanguage = settingsService.GetLanguage();
                    
                    if (currentLanguage != languageCode)
                    {
                        settingsService.SetLanguage(languageCode);
                        LoggingService.Instance.Info($"Language changed to: {languageCode}");
                        
                        NotificationService.Instance.ShowConfirm(
                            LocalizationService.Instance.GetString("settings_restart_required"),
                            LocalizationService.Instance.GetString("settings_restart_now") + "?",
                            onConfirm: () =>
                            {
                                var processPath = Environment.ProcessPath;
                                if (string.IsNullOrEmpty(processPath))
                                {
                                    processPath = Process.GetCurrentProcess().MainModule?.FileName;
                                }
                                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = processPath,
                                        UseShellExecute = true
                                    });
                                }
                                Application.Current.Shutdown();
                            },
                            onCancel: () =>
                            {
                                _isInitializing = true;
                                LoadSettings();
                                _isInitializing = false;
                            }
                        );
                    }
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
                LocalizationService.Instance.GetString("SettingsCleanTemp"),
                LocalizationService.Instance.GetString("SettingsCleanTemp") + "?",
                onConfirm: () =>
                {
                    try
                    {
                        DownloadService.Instance.CleanTempFiles();
                        NotificationService.Instance.ShowSuccess(
                            LocalizationService.Instance.GetString("SettingsTitle"),
                            LocalizationService.Instance.GetString("notification_success_title"));
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Error("Failed to clean temp files", ex);
                        NotificationService.Instance.ShowError(
                            LocalizationService.Instance.GetString("SettingsTitle"),
                            LocalizationService.Instance.GetString("notification_error_title"));
                    }
                }
            );
        }
    }
}