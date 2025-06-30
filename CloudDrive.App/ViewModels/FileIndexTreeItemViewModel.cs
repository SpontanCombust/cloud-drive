using CloudDrive.App.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;

namespace CloudDrive.App.ViewModels
{
    public partial class FileIndexTreeItemViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private Guid fileId;

        /// <summary>
        /// Path relative to the watched folder
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FileName))]
        private string filePath;

        [ObservableProperty]
        private bool deleted;

        [ObservableProperty]
        private bool isDir;

        [ObservableProperty]
        private ObservableCollection<FileIndexTreeItemViewModel> subindices;


        public string FileName => Path.GetFileName(FilePath) ?? "";

        public bool IsValid => FileId != Guid.Empty;


        public static readonly Comparison<FileIndexTreeItemViewModel> FILE_NAME_COMPARISON = (x, y) => string.Compare(x.FileName, y.FileName, StringComparison.OrdinalIgnoreCase);


        public FileIndexTreeItemViewModel(string filePath, bool isDir, bool deleted, Guid fileId)
        {
            this.fileId = fileId;
            this.filePath = filePath;
            this.isDir = isDir;
            this.deleted = deleted;
            this.subindices = new();
        }

        public FileIndexTreeItemViewModel(string filePath, bool isDir, bool deleted)
        {
            this.fileId = Guid.Empty;
            this.filePath = filePath;
            this.isDir = isDir;
            this.deleted = deleted;
            this.subindices = new();
        }

        public FileIndexTreeItemViewModel(string filePath, bool isDir)
        {
            this.fileId = Guid.Empty;
            this.filePath = filePath;
            this.isDir = isDir;
            this.deleted = false;
            this.subindices = new();
        }


        /// <summary>
        /// Get index for the first component in this index's path. It will not contain a valid FileId.
        /// </summary>
        public FileIndexTreeItemViewModel ExtractRootIndex()
        {
            var pathComponents = FilePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (pathComponents.Length > 1)
            {
                var rootPath = pathComponents[0];
                return new FileIndexTreeItemViewModel(rootPath, true, Deleted);
            }
            else
            {
                return new FileIndexTreeItemViewModel(FilePath, IsDir, Deleted);
            }            
        }

        /// <summary>
        /// Does this index contain only one component in its path?
        /// </summary>
        public bool IsRootIndex()
        {
            var pathComponents = FilePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return pathComponents.Length == 1;
        }


        public bool TryInsertSubindex(FileIndexTreeItemViewModel treeItem)
        {
            var subpath = Path.GetRelativePath(FilePath, treeItem.FilePath);

            // treeItem does not represent a subpath
            if (string.IsNullOrEmpty(subpath) || subpath == treeItem.FilePath)
            {
                return false;
            }
            // Already at correct level - update this item's properties
            else if (subpath == ".")
            {
                FileId = treeItem.FileId;
                IsDir = treeItem.IsDir;
                Deleted = treeItem.Deleted;
                return true;
            }

            var subpathComponents = subpath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Direct child - add or update at current level
            if (subpathComponents.Length == 1)
            {
                var existing = Subindices.FirstOrDefault(x => 
                    string.Equals(x.FilePath, treeItem.FilePath, StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    existing.FileId = treeItem.FileId;
                    existing.IsDir = treeItem.IsDir;
                    existing.Deleted = treeItem.Deleted;
                }
                else
                {
                    InsertDirectSubindex(treeItem);
                }

                return true;
            }

            // Find or create the next level in hierarchy
            var nextLevelPath = Path.Combine(FilePath, subpathComponents[0]);
            var nextLevel = Subindices.FirstOrDefault(x => 
                string.Equals(x.FilePath, nextLevelPath, StringComparison.OrdinalIgnoreCase));

            if (nextLevel == null)
            {
                nextLevel = new FileIndexTreeItemViewModel(nextLevelPath, true, treeItem.Deleted);
                InsertDirectSubindex(nextLevel);
            }

            return nextLevel.TryInsertSubindex(treeItem);
        }

        private void InsertDirectSubindex(FileIndexTreeItemViewModel treeItem)
        {
            Subindices.InsertSorted(treeItem, FILE_NAME_COMPARISON);
        }


        public bool IsSubindexOf(FileIndexTreeItemViewModel treeItem)
        {
            if (!treeItem.IsDir)
                return false;

            var relativePath = Path.GetRelativePath(treeItem.FilePath, FilePath);
            return !string.IsNullOrEmpty(relativePath) 
                && relativePath != "." 
                && !relativePath.Contains("..")
                && !relativePath.StartsWith(Path.DirectorySeparatorChar)
                && !relativePath.StartsWith(Path.AltDirectorySeparatorChar);
        }
    }
}
