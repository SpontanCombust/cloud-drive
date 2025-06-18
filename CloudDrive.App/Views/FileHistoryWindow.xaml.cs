using CloudDrive.App.Factories;
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using CloudDrive.App.Utils;
using CloudDrive.App.ViewModels;
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
    public partial class FileHistoryWindow : Window
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly ILogger<FileHistoryWindow> _logger;
        private readonly ISyncService _syncService;
        private readonly IFileSystemWatcher _fsWatcher;

        public FileHistoryWindowViewModel ViewModel;

        public FileHistoryWindow(WebAPIClientFactory apiFactory, ILogger<FileHistoryWindow> logger, ISyncService syncService, IFileSystemWatcher fsWatcher)
        {
            _apiFactory = apiFactory;
            _logger = logger;
            _syncService = syncService;
            _fsWatcher = fsWatcher;

            ViewModel = new FileHistoryWindowViewModel();
            DataContext = ViewModel;

            InitializeComponent();
        }


        private async void FileIndexTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileIndexTreeItemViewModel treeItem && treeItem.IsValid)
            {
                ViewModel.SelectedFileItem = treeItem;
                await LoadFileVersions(treeItem.FileId);
            }
            else
            {
                ViewModel.SelectedFileItem = null;
                ViewModel.SelectedFileVersionItem = null;
                ViewModel.FileVersionItems.Clear();
            }
        }

        private async Task LoadFileVersions(Guid fileId)
        {
            try
            {
                ViewModel.FileVersionItems.Clear();

                var api = _apiFactory.Create();

                var fileInfo = await api.GetFileInfoAsync(fileId);
                var versionsInfo = await api.GetFileVersionInfosForFileAsync(fileId);

                foreach (var version in versionsInfo.FileVersionsInfos)
                {
                    ViewModel.FileVersionItems.InsertSorted(
                        new FileVersionListItemViewModel(fileInfo, version), 
                        (x, y) => y.VersionNr - x.VersionNr
                    );
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
            if (ViewModel.SelectedFileItem == null || ViewModel.SelectedFileVersionItem == null) 
                return;

            try
            {
                // W czasie wykonywania operacji przywracania plików programatycznie zmieniany jest stan systemu plików, np. pobierane i zapisywane są pliki.
                // Zmiana tego stanu może zostać wykryta przez FileSystemWatcher, a w efekcie wykonanie niepotrzebnych operacji na plikach,
                // nad którymi program w tym momencie pracuje. Dlatego też należy wyłączyć FileSystemWatchera na okres przywracania wersji plików.
                _fsWatcher.Stop();

                if (ViewModel.SelectedFileItem.IsDir)
                {
                    await _syncService.RestoreFolderFromRemoteAsync(ViewModel.SelectedFileItem.FileId, ViewModel.SelectedFileVersionItem.FileVersionId);
                }
                else
                {
                    await _syncService.RestoreFileFromRemoteAsync(ViewModel.SelectedFileItem.FileId, ViewModel.SelectedFileVersionItem.FileVersionId);
                }

                MessageBox.Show($"Przywrócono wersję {ViewModel.SelectedFileVersionItem.VersionNr}!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // if the item was deleted reload the whole view...
                if (ViewModel.SelectedFileItem.Deleted)
                {
                    await FillFileIndexTree();
                }
                // ...if not just reload versions for the selected file
                else
                {
                    await LoadFileVersions(ViewModel.SelectedFileItem.FileId);
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

        public async Task FillFileIndexTree()
        {
            try
            {
                ViewModel.FileIndexTree.Clear();

                var api = _apiFactory.Create();
                //FIXME should rather make use of the index stored in SyncService
                SyncAllExtResponse resp = await api.SyncAllExtAsync(true);

                var fvs = resp.CurrentFileVersionsInfosExt;

                foreach (var fv in fvs)
                {
                    var treeItem = new FileIndexTreeItemViewModel(fv.ClientFilePath(), fv.File.IsDir, fv.File.Deleted, fv.File.FileId);
                    ViewModel.FileIndexTree.InsertIndex(treeItem);
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
    }
}