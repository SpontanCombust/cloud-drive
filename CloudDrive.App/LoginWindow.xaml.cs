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
        private static string _authToken = "";
        public static string AuthToken => _authToken;

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

            var success = await SendRequestAsync($"{_serverUrl}/auth/signin", email, password);
            if (success)
            {
                StatusTextBlock.Text = "Logowanie zakończone sukcesem!";

                SyncButton.IsEnabled = true; 

                await SyncFiles();
            }
            else
            {
                StatusTextBlock.Text = "Błąd podczas logowania!";
            }
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

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<LoginResponse>(content);
                    _authToken = responseData?.Token; // zakładam, że obiekt ma właściwość Token
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private class LoginResponse
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }

        private async Task SyncFiles()
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                StatusTextBlock.Text = "Brak tokena autoryzacyjnego!";
                return;
            }

            var client = new WebAPIClient(_serverUrl)
            {
                // przekazanie tokena w nagłówkach
                HttpClient = new HttpClient
                {
                    DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", _authToken) }
                }
            };

            try
            {
                var metadataList = await client.SyncAsync(); // GET /sync
                                                             // Zapisujemy metadataList do lokalnej listy plików
                MessageBox.Show("Synchronizacja zakończona sukcesem!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd synchronizacji: " + ex.Message);
            }
        }

        private async Task UploadFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(filePath)) return;

            var client = new WebAPIClient(_serverUrl)
            {
                HttpClient = new HttpClient
                {
                    DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", _authToken) }
                }
            };

            try
            {
                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);

                var result = await client.CreateFileAsync(fileStream, fileName);

                MessageBox.Show($"Wysłano plik: {fileName} (ID: {result.Id})");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wysyłania pliku: " + ex.Message);
            }
        }

        private async Task DownloadFileAsync(string fileId, string destinationPath)
        {
            if (string.IsNullOrEmpty(_authToken)) return;

            var client = new WebAPIClient(_serverUrl)
            {
                HttpClient = new HttpClient
                {
                    DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", _authToken) }
                }
            };

            try
            {
                using var stream = await client.GetLatestFileVersionAsync(fileId);

                using var output = File.Create(destinationPath);
                await stream.CopyToAsync(output);

                MessageBox.Show($"Pobrano plik do: {destinationPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania pliku: " + ex.Message);
            }
        }

        private async void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AuthToken))
            {
                StatusTextBlock.Text = "Nie jesteś zalogowany!";
                return;
            }

            try
            {
                var syncService = new SyncService(_serverUrl, AuthToken, _watchedFolderPath);
                await syncService.SynchronizeAsync();
                StatusTextBlock.Text = "Synchronizacja zakończona!";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Błąd synchronizacji: {ex.Message}";
            }
        }

        private void BackToSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().Show();
            this.Close();
        }
    }
}
