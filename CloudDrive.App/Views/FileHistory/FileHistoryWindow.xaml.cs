using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using CloudDrive.App.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CloudDrive.App.Views.FileHistory
{
    /// <summary>
    /// Interaction logic for FileHistoryWindow.xaml
    /// </summary>
    public partial class FileHistoryWindow : Window, INotifyPropertyChanged
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly ILogger<FileHistoryWindow> _logger;
        private readonly ISyncService _syncService;
        private readonly IFileSystemWatcher _fsWatcher;


        public FileIndexTreeViewModel TreeViewModel { get; }

        public FileIndexTreeItemViewModel? SelectedFileItem { get; set; }


        public ObservableCollection<FileVersionListItemViewModel> FileVersionItems { get; } = new();

        private FileVersionListItemViewModel? _selectedFileVersionItem;
        public FileVersionListItemViewModel? SelectedFileVersionItem
        {
            get => _selectedFileVersionItem;
            set
            {
                if (_selectedFileVersionItem != value)
                {
                    _selectedFileVersionItem = value;
                    OnPropertyChanged(nameof(SelectedFileVersionItem));
                    OnPropertyChanged(nameof(IsRestorableFileVersionItemSelected));
                }
            }
        }

        public bool IsRestorableFileVersionItemSelected
        {
            get => _selectedFileVersionItem != null 
                && ((SelectedFileItem?.Deleted ?? false) || !_selectedFileVersionItem.Active);
        }


        public FileHistoryWindow(WebAPIClientFactory apiFactory, ILogger<FileHistoryWindow> logger, ISyncService syncService, IFileSystemWatcher fsWatcher)
        {
            _apiFactory = apiFactory;
            _logger = logger;
            _syncService = syncService;
            _fsWatcher = fsWatcher;

            InitializeComponent();

            TreeViewModel = new FileIndexTreeViewModel();
            SelectedFileItem = null;
            SelectedFileVersionItem = null;

            DataContext = this;
            FileIndexTreeView.DataContext = TreeViewModel;

            Task.Run(FillFileIndexTree).Wait();
        }

        private async void FileIndexTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileIndexTreeItemViewModel treeItem && treeItem.IsValid)
            {
                SelectedFileItem = treeItem;
                await LoadFileVersions(treeItem.FileId);
            }
            else
            {
                SelectedFileItem = null;
                SelectedFileVersionItem = null;
                FileVersionItems.Clear();
            }
        }

        private async Task LoadFileVersions(Guid fileId)
        {
            try
            {
                FileVersionItems.Clear();

                var api = _apiFactory.Create();

                var fileInfo = await api.GetFileInfoAsync(fileId);
                var versionsInfo = await api.GetFileVersionInfosForFileAsync(fileId);

                foreach (var version in versionsInfo.FileVersionsInfos)
                {
                    FileVersionItems.InsertSorted(new FileVersionListItemViewModel
                    {
                        FileVersionId = version.FileVersionId,
                        ClientPath = version.ClientFilePath(),
                        VersionNr = version.VersionNr,
                        Md5 = version.Md5,
                        SizeBytes = version.SizeBytes,
                        CreatedDate = version.CreatedDate.DateTime,
                        Active = fileInfo.ActiveFileVersionId == version.FileVersionId,
                    }, (x, y) => y.VersionNr - x.VersionNr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Błąd pobierania informacji o wersjach pliku: {}", ex.Message);
                MessageBox.Show($"Błąd pobierania informacji o wersjach pliku: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnApplyVersionClick(object sender, RoutedEventArgs e)
        {
            if (SelectedFileItem == null || SelectedFileVersionItem == null) 
                return;

            try
            {
                // W czasie wykonywania operacji przywracania plików programatycznie zmieniany jest stan systemu plików, np. pobierane i zapisywane są pliki.
                // Zmiana tego stanu może zostać wykryta przez FileSystemWatcher, a w efekcie wykonanie niepotrzebnych operacji na plikach,
                // nad którymi program w tym momencie pracuje. Dlatego też należy wyłączyć FileSystemWatchera na okres przywracania wersji plików.
                _fsWatcher.Stop();

                if (SelectedFileItem.IsDir)
                {
                    await _syncService.RestoreFolderFromRemoteAsync(SelectedFileItem.FileId, SelectedFileVersionItem.FileVersionId);
                }
                else
                {
                    await _syncService.RestoreFileFromRemoteAsync(SelectedFileItem.FileId, SelectedFileVersionItem.FileVersionId);
                }

                MessageBox.Show($"Przywrócono wersję {SelectedFileVersionItem.VersionNr}!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // if the item was deleted reload the whole view...
                if (SelectedFileItem.Deleted)
                {
                    await FillFileIndexTree();
                }
                // ...if not just reload versions for the selected file
                else
                {
                    await LoadFileVersions(SelectedFileItem.FileId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Błąd przywracania wersji: {}", ex.Message);
                MessageBox.Show($"Błąd przywracania wersji: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _fsWatcher.Start();
            }
        }

        private async Task FillFileIndexTree()
        {
            try
            {
                TreeViewModel.Clear();

                var api = _apiFactory.Create();
                //FIXME should rather make use of the index stored in SyncService
                SyncAllExtResponse resp = await api.SyncAllExtAsync(true);

                var fvs = resp.CurrentFileVersionsInfosExt;

                foreach (var fv in fvs)
                {
                    var treeItem = new FileIndexTreeItemViewModel(fv.ClientFilePath(), fv.IsDir, fv.Deleted, fv.FileId);
                    TreeViewModel.InsertIndex(treeItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Błąd pobrania historii plików: {}", ex.Message);
                MessageBox.Show($"Błąd pobrania historii plików: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await FillFileIndexTree();
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}