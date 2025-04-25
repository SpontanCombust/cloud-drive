using CloudDrive.App.Model;
using CloudDrive.App.Services;

namespace CloudDrive.App.ServicesImpl
{
    public class LogRelayService : ILogRelayService
    {
        public event EventHandler<LogMessageEventArgs>? LogAdded;

        public void Relay(LogMessageEventArgs ev)
        {
            LogAdded?.Invoke(this, ev);
        }
    }
}
