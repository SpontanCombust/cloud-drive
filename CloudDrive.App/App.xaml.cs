using System;
using System.Windows;
using System.IO;
using CloudDrive.App.Views;

namespace CloudDrive.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudDrive", "settings.json");

            if (File.Exists(settingsFilePath))
            {
                new LoginWindow().Show();
            }
            else
            {
                new SettingsWindow().Show();
            }
        }
    }
}
