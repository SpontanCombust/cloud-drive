using CloudDrive.App.Views;

namespace CloudDrive.App.Services
{
    public interface IViewLocator
    {
        LoginWindow LoginWindow();
        SettingsWindow SettingsWindow();
    }
}
