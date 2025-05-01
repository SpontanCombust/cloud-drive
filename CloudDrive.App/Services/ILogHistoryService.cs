using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface ILogHistoryService
    {
        int Capacity { get; set; }

        IReadOnlyCollection<LogMessageEventArgs> GetHistory();
        void ClearHistory();
    }
}
