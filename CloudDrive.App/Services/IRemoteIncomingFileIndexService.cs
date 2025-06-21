using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface IRemoteIncomingFileIndexService
    {
        Task FetchAsync();

        IEnumerable<RemoteIncomingFileIndexEntry> FindAll();
    }
}
