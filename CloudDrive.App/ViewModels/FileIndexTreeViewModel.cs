using CloudDrive.App.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace CloudDrive.App.ViewModels
{
    public partial class FileIndexTreeViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<FileIndexTreeItemViewModel> active;

        [ObservableProperty]
        private ObservableCollection<FileIndexTreeItemViewModel> archived;


        public FileIndexTreeViewModel()
        {
            this.active = new();
            this.archived = new();
        }


        public void Clear()
        {
            Active.Clear();
            Archived.Clear();
        }

        public void InsertIndex(FileIndexTreeItemViewModel treeItem)
        {
            if (!treeItem.Deleted)
            {
                InsertIndexIntoCollection(treeItem, Active);
            }
            else
            {
                InsertIndexIntoCollection(treeItem, Archived);
            }
        }


        private FileIndexTreeItemViewModel? FindItemByPath(string path, ObservableCollection<FileIndexTreeItemViewModel> items)
        {
            foreach (var item in items)
            {
                if (string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    return item;

                var found = FindItemByPath(path, item.Subindices);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void InsertIndexIntoCollection(FileIndexTreeItemViewModel treeItem, ObservableCollection<FileIndexTreeItemViewModel> collection)
        {
            // Check if item with this exact path already exists anywhere in the collection
            var existingItem = FindItemByPath(treeItem.FilePath, collection);
            if (existingItem != null)
            {
                // Update existing item's properties
                existingItem.FileId = treeItem.FileId;
                existingItem.IsDir = treeItem.IsDir;
                existingItem.Deleted = treeItem.Deleted;
                return;
            }

            // Try to find a parent directory that could contain this item
            bool inserted = false;
            foreach (var item in collection)
            {
                if (treeItem.IsSubindexOf(item))
                {
                    inserted = item.TryInsertSubindex(treeItem);
                    if (inserted) 
                        break;
                }
            }

            if (!inserted)
            {
                if (treeItem.IsRootIndex())
                {
                    collection.InsertSorted(treeItem, FileIndexTreeItemViewModel.FILE_NAME_COMPARISON);
                }
                else
                {
                    // Initialize directory structure for this item
                    var rootIndex = treeItem.ExtractRootIndex();
                    // Check if root directory already exists
                    var existingRoot = collection.FirstOrDefault(x => 
                        string.Equals(x.FilePath, rootIndex.FilePath, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingRoot != null)
                    {
                        existingRoot.TryInsertSubindex(treeItem);
                    }
                    else
                    {
                        rootIndex.TryInsertSubindex(treeItem);
                        collection.InsertSorted(rootIndex, FileIndexTreeItemViewModel.FILE_NAME_COMPARISON);
                    }
                }
            }
        }
    }
}
