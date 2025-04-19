using System.IO;

namespace CloudDrive.App.Model
{
    public class WatchedFileSystemPath
    {
        /// <summary>
        /// Full path to a regular file or a directory. Must be a subpath of <see cref="WatchedFileSystemPath.WatchedFolder">WatchedFolder</see>
        /// </summary>
        public readonly string Full;
        /// <summary>
        /// Full path to the folder watched by the client
        /// </summary>
        public readonly string WatchedFolder;
        /// <summary>
        /// If this path denotes a directory
        /// </summary>
        public readonly bool IsDirectory;


        /// <summary>
        /// Constructs path information about a regular file or directory watched by the client
        /// </summary>
        /// <param name="fullPath">
        /// Full path to a regular file or a directory. Must be a subpath of watchedFolder
        /// </param>
        /// Full path to the folder watched by the client
        /// <param name="watchedFolder">
        /// </param>
        /// <param name="isDirectory">
        /// If this path denotes a directory
        /// </param>
        public WatchedFileSystemPath(string fullPath, string watchedFolder, bool isDirectory)
        {
            ArgumentNullException.ThrowIfNull(fullPath);
            ArgumentNullException.ThrowIfNull(watchedFolder);

            if (!Path.IsPathRooted(fullPath))
                throw new ArgumentException("fullPath must be a full, rooted path", fullPath);
            if (!fullPath.StartsWith(watchedFolder))
                throw new ArgumentException("fullPath must be a subpath of watchedFolder", fullPath);

            Full = fullPath.TrimEnd(Path.PathSeparator);
            WatchedFolder = watchedFolder.TrimEnd(Path.PathSeparator);
            IsDirectory = isDirectory;
        }

        /// <summary>
        /// A relative from <see cref="WatchedFolder"/> to <see cref="Full"/>
        /// </summary>
        public string Relative
        {
            get
            {
                return Path.GetRelativePath(WatchedFolder, Full);
            }
        }

        public string FileName
        {
            get
            {
                return Path.GetFileName(Full);
            }
        }

        public string? RelativeParentDir
        {
            get
            {
                return Path.GetDirectoryName(Relative);
            }
        }

        public string FullParentDir
        {
            get
            {
                return Path.GetDirectoryName(Full)!;
            }
        }

        public bool Exists
        {
            get
            {
                return Path.Exists(Full);
            }
        }


        public override bool Equals(object? obj)
        {
            return obj is WatchedFileSystemPath path && Equals(path);
        }

        public bool Equals(WatchedFileSystemPath other)
        {
            return WatchedFolder == other.WatchedFolder
                && Full == other.Full;
        }

        public override int GetHashCode()
        {
            return Full.GetHashCode();
        }
    }
}
