using CloudDrive.App.Factories;
using CloudDrive.App.Services;
using CloudDrive.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CloudDrive.App.ServicesImpl
{
    public class ViewLocator : IViewLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public LoginPage LoginPage()
        {
            var accessTokenHolder = _serviceProvider.GetRequiredService<IAccessTokenHolder>();
            var apiFactory = _serviceProvider.GetRequiredService<WebAPIClientFactory>();
            return new LoginPage(this, accessTokenHolder, apiFactory);
        }

        public SettingsPage SettingsPage()
        {
            var userSettingsService = _serviceProvider.GetRequiredService<IUserSettingsService>();
            return new SettingsPage(userSettingsService, this);
        }

        public StatusPage StatusPage()
        {
            var logRelayService = _serviceProvider.GetRequiredService<ILogRelayService>();
            var logHistoryService = _serviceProvider.GetRequiredService<ILogHistoryService>();
            var syncService = _serviceProvider.GetRequiredService<ISyncService>();
            return new StatusPage(logRelayService, logHistoryService, syncService);
        }
    }
}
