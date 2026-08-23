using System.Diagnostics;
using System.Windows.Controls;

namespace ReduxInstaller.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void DeveloperButton_Click(object sender, System.Windows.RoutedEventArgs e)
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
    }
}