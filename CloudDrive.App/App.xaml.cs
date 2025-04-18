using System;
using System.Windows;
using System.IO;
using CloudDrive.App.Views;
using Microsoft.Extensions.DependencyInjection;
using CloudDrive.App.Services;
using CloudDrive.App.ServicesImpl;

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
            services.AddSingleton<IUserSettingsService, AppDataUserSettingsService>();
            services.AddSingleton<IViewLocator, ViewLocator>();
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var viewLocator = Services.GetRequiredService<IViewLocator>();
            var settingsService = Services.GetRequiredService<IUserSettingsService>();
            if (settingsService.SettingsWereSaved())
            {
                viewLocator.LoginWindow().Show();
            }
            else
            {
                viewLocator.SettingsWindow().Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dispose of services if needed
            if (Services is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
