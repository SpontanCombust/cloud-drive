using CloudDrive.App.Model;

namespace CloudDrive.App.Services
{
    public interface IRemoteIncomingFileIndexService
    {
        Task FetchAsync();
        DateTime? LastFetchServerTime();

        IEnumerable<RemoteIncomingFileIndexEntry> FindAll();
    }
}
