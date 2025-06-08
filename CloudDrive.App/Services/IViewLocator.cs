using CloudDrive.App.Views;
using CloudDrive.App.Views.FileHistory;

namespace CloudDrive.App.Services
{
    public interface IViewLocator
    {
        LoginPage LoginPage();
        SettingsPage SettingsPage();
        StatusPage StatusPage();
        FileHistoryWindow FileHistoryWindow();
    }
}
