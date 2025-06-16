using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CloudDrive.App.Views
{
    public partial class MainWindow : Window
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IViewLocator _viewLocator;

        public MainWindow(IUserSettingsService userSettingsService, IViewLocator viewLocator)
        {
            InitializeComponent();

            _userSettingsService = userSettingsService;
            _viewLocator = viewLocator;

            try
            {
                if (userSettingsService.SettingsWereSaved())
                {
                    var loginPage = _viewLocator.LoginPage();
                    MainFrame.Content = loginPage;
                }
                else
                {
                    var settingsPage = _viewLocator.SettingsPage();
                    MainFrame.Content = settingsPage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd podczas ładowania aplikacji: " + ex.Message);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Cancel the close and minimize to tray instead
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void SystemTrayIconRestore_Click(object sender, RoutedEventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void SystemTrayIconClose_Click(object sender, RoutedEventArgs e)
        {
            SystemTrayIcon.Visibility = Visibility.Collapsed;
            Application.Current.Shutdown();
        }

        private void SystemTrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
    }
}
