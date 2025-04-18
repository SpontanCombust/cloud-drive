using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Net.Http.Headers;
using CloudDrive.App.Services;
using CloudDrive.App.Factories;


namespace CloudDrive.App.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IUserSettingsService _userSettingsService;
        private readonly IViewLocator _viewLocator;
        private readonly IAccessTokenHolder _accessTokenHolder;
        private readonly WebAPIClient _api;
        

        public LoginWindow(
            IUserSettingsService userSettingsService, 
            IViewLocator viewLocator, 
            IAccessTokenHolder authTokenHolder, 
            WebAPIClient api)
        {
            InitializeComponent();

            _userSettingsService = userSettingsService;
            _viewLocator = viewLocator;
            _accessTokenHolder = authTokenHolder;
            _api = api;
        }


        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text.Trim();
            var password = PasswordBox.Password.Trim();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Text = "Proszę wypełnić wszystkie pola!";
                return;
            }

            try
            {
                var resp = await _api.SignUpAsync(email, password); // response contains nothing for now

                StatusTextBlock.Text = "Rejestracja zakończona sukcesem!";
            }
            catch (ApiException ex)
            {
                StatusTextBlock.Text = "Błąd rejestracji: " + ex.Response;
            }

        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text.Trim();
            var password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Text = "Proszę wypełnić wszystkie pola!";
                return;
            }

            try
            {
                var resp = await _api.SignInAsync(email, password);
                _accessTokenHolder.HoldAccessToken(resp.AccessToken);

                StatusTextBlock.Text = "Logowanie zakończone sukcesem!";
            }
            catch (ApiException ex)
            {
                StatusTextBlock.Text = "Błąd logowania: " + ex.Response;
            }

        }


        private async Task SyncFiles()
        {
            try
            {
                var metadataList = await _api.SyncAllAsync(); // GET /sync
                                                             // Zapisujemy metadataList do lokalnej listy plików
                MessageBox.Show("Synchronizacja zakończona sukcesem!");
            }
            catch (ApiException ex)
            {
                MessageBox.Show("Błąd synchronizacji: " + ex.Response);
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
