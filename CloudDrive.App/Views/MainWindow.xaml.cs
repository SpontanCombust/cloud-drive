using CloudDrive.App.Services;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IViewLocator _viewLocator;

        public MainWindow(IUserSettingsService userSettingsService, IViewLocator viewLocator)
        {
            InitializeComponent();

            _userSettingsService = userSettingsService;
            _viewLocator = viewLocator;

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
    }
}
