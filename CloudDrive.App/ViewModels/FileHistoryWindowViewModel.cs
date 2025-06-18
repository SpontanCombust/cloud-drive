using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDrive.App.ViewModels
{
    public partial class FileHistoryWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private FileIndexTreeViewModel fileIndexTree;

        [ObservableProperty]
        private FileIndexTreeItemViewModel? selectedFileItem;

        [ObservableProperty]
        private ObservableCollection<FileVersionListItemViewModel> fileVersionItems;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRestorableFileVersionItemSelected))]
        private FileVersionListItemViewModel? selectedFileVersionItem;


        public bool IsRestorableFileVersionItemSelected =>
            SelectedFileVersionItem != null
            && ((SelectedFileItem?.Deleted ?? false) || !SelectedFileVersionItem.Active);


        public FileHistoryWindowViewModel()
        {
            fileIndexTree = new FileIndexTreeViewModel();
            selectedFileItem = null;

            fileVersionItems = new();
            selectedFileVersionItem = null;
        }
    }
}
