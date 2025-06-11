using System.IO;

namespace CloudDrive.App.Utils
{
    static class FileVersionDTOExtensions
    {
        public static string ClientPath(this FileVersionDTO fv)
        {
            return fv.ClientDirPath != null
                ? Path.Combine(fv.ClientDirPath, fv.ClientFileName)
                : fv.ClientFileName;
        }
    }
}
