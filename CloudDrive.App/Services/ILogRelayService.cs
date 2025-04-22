using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ILogRelayService
    {
        event EventHandler<LogMessageEventArgs>? LogAdded;

        void Relay(LogMessageEventArgs e);
    }
}
