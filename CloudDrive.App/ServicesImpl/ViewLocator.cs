using CloudDrive.App.Services;
using CloudDrive.App.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDrive.App.ServicesImpl
{
    public class ViewLocator : IViewLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public LoginWindow LoginWindow()
        {
            var userSettingsService = _serviceProvider.GetRequiredService<IUserSettingsService>();
            return new LoginWindow(userSettingsService, this);
        }

        public SettingsWindow SettingsWindow()
        {
            var userSettingsService = _serviceProvider.GetRequiredService<IUserSettingsService>();
            return new SettingsWindow(userSettingsService, this);
        }
    }
}
