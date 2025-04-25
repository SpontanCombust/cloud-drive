using CloudDrive.App.Views;

namespace CloudDrive.App.Services
{
    public interface IViewLocator
    {
        LoginPage LoginPage();
        SettingsPage SettingsPage();
        StatusPage StatusPage();
    }
}
