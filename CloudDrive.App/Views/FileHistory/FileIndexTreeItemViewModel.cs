using CloudDrive.App.Utils;
using System.Collections.ObjectModel;
using System.IO;

namespace CloudDrive.App.Views.FileHistory
{
    public class FileIndexTreeItemViewModel
    {
        public Guid FileId { get; }
        /// <summary>
        /// Path relative to the watched folder
        /// </summary>
        public string FilePath { get; }
        public bool Deleted { get; }
        public ObservableCollection<FileIndexTreeItemViewModel> Subindices { get; } = new();


        public FileIndexTreeItemViewModel(string filePath, bool deleted, Guid fileId)
        {
            FileId = fileId;
            FilePath = filePath;
            Deleted = deleted;
        }

        public FileIndexTreeItemViewModel(string filePath)
        {
            FileId = Guid.Empty;
            FilePath = filePath;
            Deleted = false;
        }


        public string FileName => Path.GetFileName(FilePath) ?? "";

        public bool IsValid => FileId != Guid.Empty;


        public bool TryInsertSubindex(FileIndexTreeItemViewModel treeItem)
        {
            var subpath = Path.GetRelativePath(FilePath, treeItem.FilePath);

            // Already at correct level
            if (string.IsNullOrEmpty(subpath) || subpath == ".")
            {
                return false;
            }

            var pathComponents = subpath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Direct child - add to current level
            if (pathComponents.Length == 1)
            {
                InsertDirectSubindex(treeItem);
                return true;
            }

            // Find or create the next level in hierarchy
            var nextLevelPath = Path.Combine(FilePath, pathComponents[0]);
            var nextLevel = Subindices.FirstOrDefault(x => x.FilePath == nextLevelPath);

            if (nextLevel == null)
            {
                nextLevel = new FileIndexTreeItemViewModel(nextLevelPath);
                InsertDirectSubindex(nextLevel);
            }

            return nextLevel.TryInsertSubindex(treeItem);
        }

        private void InsertDirectSubindex(FileIndexTreeItemViewModel treeItem)
        {
            Subindices.InsertSorted(treeItem, (x, y) => string.Compare(x.FileName, y.FileName, StringComparison.OrdinalIgnoreCase));
        }


        public bool IsSubindex(FileIndexTreeItemViewModel treeItem)
        {
            var subpath = Path.GetRelativePath(FilePath, treeItem.FilePath);
            return !string.IsNullOrEmpty(subpath) && subpath != "." && !subpath.Contains("..");
        }
    }
}
