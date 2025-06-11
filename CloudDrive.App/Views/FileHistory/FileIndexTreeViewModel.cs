using CloudDrive.App.Utils;
using System.Collections.ObjectModel;

namespace CloudDrive.App.Views.FileHistory
{
    public class FileIndexTreeViewModel
    {
        public ObservableCollection<FileIndexTreeItemViewModel> Active { get; } = new();
        public ObservableCollection<FileIndexTreeItemViewModel> Archived { get; } = new();


        public void Clear()
        {
            Active.Clear();
            Archived.Clear();
        }

        public void InsertIndex(FileIndexTreeItemViewModel treeItem)
        {
            if (treeItem.Deleted)
            {
                InsertArchivedIndex(treeItem);
            }
            else
            {
                InsertActiveIndex(treeItem);
            }
        }


        private void InsertActiveIndex(FileIndexTreeItemViewModel treeItem)
        {
            bool inserted = false;
            foreach (var item in Active)
            {
                if (item.IsSubindex(treeItem))
                {
                    item.TryInsertSubindex(treeItem);
                    inserted = true;
                    break;
                }
            }

            if (!inserted)
            {
                Active.InsertSorted(treeItem, (x, y) => string.Compare(x.FileName, y.FileName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void InsertArchivedIndex(FileIndexTreeItemViewModel treeItem)
        {
            bool inserted = false;
            foreach (var item in Archived)
            {
                if (item.IsSubindex(treeItem))
                {
                    item.TryInsertSubindex(treeItem);
                    inserted = true;
                    break;
                }
            }

            if (!inserted)
            {
                Archived.InsertSorted(treeItem, (x, y) => string.Compare(x.FileName, y.FileName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
