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
using Microsoft.Extensions.DependencyInjection;


namespace CloudDrive.App.Views
{
    public partial class SettingsPage : Page
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IViewLocator _viewLocator;

        public SettingsPage(IUserSettingsService userSettingsService, IViewLocator viewLocator)
        {
            InitializeComponent();

            _userSettingsService = userSettingsService;
            _viewLocator = viewLocator;

            LoadSettings();
        }


        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new Microsoft.Win32.OpenFolderDialog();
            if (folderDialog.ShowDialog() ?? false)
            {
                FolderPathTextBox.Text = folderDialog.FolderName;
                FolderPathErrorTextBlock.Text = String.Empty;
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

                ServerUrlTextBox.Text = _userSettingsService.ServerUrl?.ToString() ?? "";
                FolderPathTextBox.Text = _userSettingsService.WatchedFolderPath?.ToString() ?? "";
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
                var url = new Uri(ServerUrlTextBox.Text);
                _userSettingsService.ServerUrl = url;
                return true;
            }
            catch (Exception ex)
            {
                ServerUrlErrorTextBlock.Text = ex.Message;
                return false;
            }
        }

        private bool ValidateAndSetFolderPath()
        {
            string folderPath = FolderPathTextBox.Text;

            if (!Directory.Exists(folderPath))
            {
                FolderPathErrorTextBlock.Text = "Folder nie istnieje";
                return false;
            }

            _userSettingsService.WatchedFolderPath = folderPath;
            return true;
        }
    }
}
