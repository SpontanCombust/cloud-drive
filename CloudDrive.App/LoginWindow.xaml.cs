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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CloudDrive.App
{
    public partial class LoginWindow : Window
    {
        private string _serverUrl = "";

        public LoginWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            string settingsFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudDrive", "settings.json");

            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                var settings = JsonConvert.DeserializeObject<SettingsWindow.ClientSettings>(json);
                _serverUrl = settings?.ServerUrl ?? string.Empty;
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Text = "Proszę wypełnić wszystkie pola!";
                return;
            }

            var success = await LoginWindow.SendRequestAsync($"{_serverUrl}/auth/signup", email, password);
            StatusTextBlock.Text = success ? "Rejestracja zakończona sukcesem!" : "Błąd podczas rejestracji!";
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Text = "Proszę wypełnić wszystkie pola!";
                return;
            }

            var success = await LoginWindow.SendRequestAsync($"{_serverUrl}/auth/signin", email, password);
            StatusTextBlock.Text = success ? "Logowanie zakończone sukcesem!" : "Błąd podczas logowania!";
        }

        private static async Task<bool> SendRequestAsync(string url, string email, string password)
        {
            using var client = new HttpClient();
            try
            {
                var formData = new MultipartFormDataContent
                    {
                        { new StringContent(email), "email" },
                        { new StringContent(password), "password" }
                    };

                var response = await client.PostAsync(url, formData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void BackToSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().Show();
            this.Close();
        }
    }
}
