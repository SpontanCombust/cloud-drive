using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Net.Http.Headers;
using CloudDrive.App.Services;


namespace CloudDrive.App.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IViewLocator _viewLocator;

        private static string _authToken = "";
        public static string AuthToken => _authToken;

        public LoginWindow(IUserSettingsService userSettingsService, IViewLocator viewLocator)
        {
            InitializeComponent();

            _userSettingsService = userSettingsService;
            _viewLocator = viewLocator;
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

            try
            {
                var success = await Api.SignUpAsync(email, password);

                StatusTextBlock.Text = "Rejestracja zakończona sukcesem!";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Nieoczekiwany błąd serwer " + ex.Message;
            }

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

            try
            {
                var success = await Api.SignInAsync(email, password);

                StatusTextBlock.Text = "Logowanie zakończone sukcesem!";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Nieoczekiwany błąd serwer " + ex.Message;
            }

        }


        private async Task SyncFiles()
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                StatusTextBlock.Text = "Brak tokena autoryzacyjnego!";
                return;
            }

            try
            {
                var metadataList = await Api.SyncAllAsync(); // GET /sync
                                                             // Zapisujemy metadataList do lokalnej listy plików
                MessageBox.Show("Synchronizacja zakończona sukcesem!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd synchronizacji: " + ex.Message);
            }
        }

        private WebAPIClient Api 
        {
            get 
            {
                HttpClient client = new HttpClient
                {
                    DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", _authToken) }
                };
                return new WebAPIClient(_userSettingsService.ServerUrl.ToString(), client);
            }
        }

        private void BackToSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = _viewLocator.SettingsWindow();
            settingsWindow.Show();
            this.Close();
        }
    }
}
