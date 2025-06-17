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

        public static FileVersionDTO TrimExt(this FileVersionExtDTO fve)
        {
            return new FileVersionDTO
            {
                FileVersionId = fve.FileVersionId,
                FileId = fve.FileId,
                ClientFileName = fve.ClientFileName,
                ClientDirPath = fve.ClientDirPath,
                SizeBytes = fve.SizeBytes,
                VersionNr = fve.VersionNr,
                CreatedDate = fve.FileVersionCreatedDate,
                Md5 = fve.Md5
            };
        }
    }
}
