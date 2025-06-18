using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CloudDrive.App.Services;
using CloudDrive.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;


namespace CloudDrive.App.Views
{
    public partial class SettingsPage : Page
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IViewLocator _viewLocator;

        private readonly SettingsPageViewModel ViewModel;

        public SettingsPage(IUserSettingsService userSettingsService, IViewLocator viewLocator)
        {
            _userSettingsService = userSettingsService;
            _viewLocator = viewLocator;

            InitializeComponent();

            ViewModel = new SettingsPageViewModel();
            DataContext = ViewModel;

            LoadSettings();
        }


        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new Microsoft.Win32.OpenFolderDialog();
            if (folderDialog.ShowDialog() ?? false)
            {
                ViewModel.FolderPath = folderDialog.FolderName;
                ViewModel.FolderPathError = string.Empty;
            }
        }

        private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAndSetServerUrl())
            {
                return;
            }
            if (!ValidateAndSetFolderPath())
            {
                return;
            }

            try
            {
                await _userSettingsService.SaveSettingsAsync();
                MessageBox.Show("Ustawienia zapisane!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas zapisu ustawień: " + ex.Message);
                return;
            }

            // Po zapisaniu ustawień przejdź do logowania
            var loginPage = _viewLocator.LoginPage();
            this.NavigationService.Navigate(loginPage);
        }

        private async void LoadSettings()
        {
            try
            {
                await _userSettingsService.LoadSettingsAsync();

                ViewModel.ServerUrl = _userSettingsService.ServerUrl?.ToString() ?? "";
                ViewModel.FolderPath = _userSettingsService.WatchedFolderPath?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas ładowania ustawień: " + ex.Message);
            }
        }

        private bool ValidateAndSetServerUrl()
        {
            try
            {
                var url = new Uri(ViewModel.ServerUrl);
                _userSettingsService.ServerUrl = url;
                return true;
            }
            catch (Exception ex)
            {
                ViewModel.ServerUrlError = ex.Message;
                return false;
            }
        }

        private bool ValidateAndSetFolderPath()
        {
            if (!Directory.Exists(ViewModel.FolderPath))
            {
                ViewModel.FolderPathError = "Folder nie istnieje";
                return false;
            }

            _userSettingsService.WatchedFolderPath = ViewModel.FolderPath;
            return true;
        }
    }
}
