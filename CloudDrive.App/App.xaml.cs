using System;
using System.Windows;
using System.IO;
using CloudDrive.App.Views;
using Microsoft.Extensions.DependencyInjection;
using CloudDrive.App.Services;
using CloudDrive.App.ServicesImpl;
using CloudDrive.App.Factories;
using System.Windows.Threading;

namespace CloudDrive.App
{
    public partial class App : Application
    {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public static IServiceProvider Services { get; private set; }
        #pragma warning restore CS8618

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<MainWindow>()
                .AddSingleton<IUserSettingsService, AppDataUserSettingsService>()
                .AddSingleton<IViewLocator, ViewLocator>()
                .AddSingleton<IAccessTokenHolder, WebAPIAccessTokenHolder>()
                .AddSingleton<WebAPIClientFactory>()
                .AddTransient<WebAPIClient>(provider =>
                {
                    var factory = provider.GetRequiredService<WebAPIClientFactory>();
                    return factory.Create();
                })
                .AddSingleton<ISyncService, SyncService>();
        }


        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            var settingsService = Services.GetRequiredService<IUserSettingsService>();
            await settingsService.LoadSettingsAsync();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dispose of services if needed
            if (Services is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Nieoczekiwany błąd aplikacji: " + e.Exception.Message);
            e.Handled = true; // prevents app crash
        }
    }
}
