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
    public partial class MainWindow : Window
    {
        // Przykładowa ścieżka do pliku konfiguracyjnego, która powinna znikną w wersji produkcjyjnej
        private string SettingsFilePath {
            get
            {
                var appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudDrive");
                Directory.CreateDirectory(appDataPath);
                var settingsPath = System.IO.Path.Combine(appDataPath, "settings.json");
                return settingsPath;
            }
        }
        private string _serverUrl;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings(); // Wczytaj ustawienia przy inicjalizacji okna
        }

        // Klasa do przechowywania ustawień klienta
        public class ClientSettings
        {
            public string ServerUrl { get; set; }
            public string FolderPath { get; set; }
        }

        // Obsługa kliknięcia przycisku do wyboru folderu
        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            // Otwórz okno dialogowe do wyboru folderu
            var folderDialog = new Microsoft.Win32.OpenFolderDialog();
            if (folderDialog.ShowDialog() ?? false)
            {
                // Ustaw wybraną ścieżkę w polu tekstowym
                FolderPathTextBox.Text = folderDialog.FolderName;
            }
        }

        // Obsługa kliknięcia przycisku do zapisywania ustawień
        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Pobierz dane z formularza
            var settings = new ClientSettings
            {
                ServerUrl = ServerUrlTextBox.Text,
                FolderPath = FolderPathTextBox.Text
            };

            _serverUrl = ServerUrlTextBox.Text;

            // Zapisz dane do pliku JSON
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);

            // Wyświetl komunikat o sukcesie
            System.Windows.MessageBox.Show("Ustawienia zostały zapisane!");
        }

        // Metoda do wczytywania ustawień z pliku
        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                // Odczytaj dane z pliku JSON
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonConvert.DeserializeObject<ClientSettings>(json);

                // Wypełnij formularz danymi z pliku
                ServerUrlTextBox.Text = settings?.ServerUrl ?? string.Empty;
                FolderPathTextBox.Text = settings?.FolderPath ?? string.Empty;
            }
        }

        // Obsługa kliknięcia przycisku rejestracji
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text;
            var password = PasswordBox.Password;

            // Sprawdź, czy pola są wypełnione
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Text = "Proszę wypełnić wszystkie pola!";
                return;
            }

            // Wyślij żądanie rejestracji
            var success = await SendRequestAsync($"{_serverUrl}/auth/signup", email, password); // Do zmiany aby skomunikować się z bazą danych
            if (success)
            {
                StatusTextBlock.Text = "Rejestracja zakończona sukcesem!";
            }
            else
            {
                StatusTextBlock.Text = "Błąd podczas rejestracji!";
            }
        }

        // Obsługa kliknięcia przycisku logowania
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text;
            var password = PasswordBox.Password;

            // Sprawdź, czy pola są wypełnione
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Text = "Proszę wypełnić wszystkie pola!";
                return;
            }

            // Wyślij żądanie logowania
            var success = await SendRequestAsync($"{_serverUrl}/auth/signin", email, password);
            if (success)
            {
                StatusTextBlock.Text = "Logowanie zakończone sukcesem!";
            }
            else
            {
                StatusTextBlock.Text = "Błąd podczas logowania!";
            }
        }

        private async Task<bool> SendRequestAsync(string url, string email, string password)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Przygotuj dane formularza
                    var formData = new MultipartFormDataContent
                    {
                        { new StringContent(email), "email" },
                        { new StringContent(password), "password" }
                    };

                    // Wyślij żądanie POST
                    var response = await client.PostAsync(url, formData);

                    if (response.IsSuccessStatusCode)
                    {
                        // Odczytaj odpowiedź
                        var responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Odpowiedź: {responseBody}");
                        return true;
                    }
                    else
                    {
                        // Odczytaj odpowiedź błędu
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Błąd: {errorResponse}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // Obsłużenie wyjątku
                    Console.WriteLine($"Błąd połączenia: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
