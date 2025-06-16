using System.IO;

namespace CloudDrive.App.Utils
{
    static class FileVersionDTOExtensions
    {
        /// <summary>
        /// Relative path to this file or directory on the client side
        /// </summary>
        public static string ClientFilePath(this FileVersionDTO fv)
        {
            return Path.Combine(
                fv.ClientDirPath ?? string.Empty,
                fv.ClientFileName ?? string.Empty
            );
        }

        /// <summary>
        /// Relative path to this file or directory on the client side
        /// </summary>
        public static string ClientFilePath(this FileVersionExtDTO fv)
        {
            return Path.Combine(
                fv.ClientDirPath ?? string.Empty,
                fv.ClientFileName ?? string.Empty
            );
        }
    }
}
