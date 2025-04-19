using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Net.Http.Headers;
using CloudDrive.App.Services;
using CloudDrive.App.Factories;


namespace CloudDrive.App.Views
{
    public partial class LoginPage : Page
    {
        private readonly IViewLocator _viewLocator;
        private readonly IAccessTokenHolder _accessTokenHolder;
        private readonly WebAPIClientFactory _apiFactory;
        

        public LoginPage(
            IViewLocator viewLocator, 
            IAccessTokenHolder authTokenHolder, 
            WebAPIClientFactory apiFactory)
        {
            InitializeComponent();

            _viewLocator = viewLocator;
            _accessTokenHolder = authTokenHolder;
            _apiFactory = apiFactory;
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
                var resp = await Api.SignUpAsync(email, password); // response contains nothing for now

                StatusTextBlock.Text = "Rejestracja zakończona sukcesem!";
            }
            catch (ApiException ex)
            {
                StatusTextBlock.Text = "Błąd rejestracji: " + ex.Response;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Błąd logowania: " + ex.Message;
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
                var resp = await Api.SignInAsync(email, password);
                _accessTokenHolder.HoldAccessToken(resp.AccessToken);

                StatusTextBlock.Text = "Logowanie zakończone sukcesem!";
            }
            catch (ApiException ex)
            {
                StatusTextBlock.Text = "Błąd logowania: " + ex.Response;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Błąd logowania: " + ex.Message;
            }

        }

        private void BackToSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsPage = _viewLocator.SettingsPage();
            this.NavigationService.Navigate(settingsPage);
        }



        private WebAPIClient Api
        {
            get
            {
                return _apiFactory.Create();
            }
        }
    }
}
