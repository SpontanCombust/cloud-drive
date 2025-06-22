using CloudDrive.App.Factories;
using CloudDrive.App.Services;
using CloudDrive.App.Views;
using CloudDrive.App.Views.FileHistory;
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
            var fileSystemWatcher = _serviceProvider.GetRequiredService<IFileSystemWatcher>();
            var benchmarkService = _serviceProvider.GetRequiredService<IBenchmarkService>();
            var autoSyncService = _serviceProvider.GetRequiredService<IAutoSyncService>();
            return new StatusPage(logRelayService, logHistoryService, syncService, this, fileSystemWatcher, benchmarkService, autoSyncService);
        }


        public FileHistoryWindow FileHistoryWindow()
        {
            return _serviceProvider.GetRequiredService<FileHistoryWindow>();
        }
    }
}
