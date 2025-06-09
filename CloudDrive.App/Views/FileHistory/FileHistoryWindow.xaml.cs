using CloudDrive.App.Factories;
using CloudDrive.App.Services;
using CloudDrive.App.Utils;
using Microsoft.Extensions.Logging;
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


        //public event PropertyChangedEventHandler? PropertyChanged;

        public FileIndexTreeViewModel TreeViewModel { get; }

        public ObservableCollection<FileVersionListItemViewModel> FileVersions { get; } = new();

        private FileVersionListItemViewModel? _selectedFileVersion;
        public FileVersionListItemViewModel? SelectedFileVersion
        {
            get => _selectedFileVersion;
            set
            {
                if (_selectedFileVersion != value)
                {
                    _selectedFileVersion = value;
                    //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFileVersion)));
                }
            }
        }


        public FileHistoryWindow(WebAPIClientFactory apiFactory, ILogger<FileHistoryWindow> logger)
        {
            _apiFactory = apiFactory;
            _logger = logger;

            InitializeComponent();

            TreeViewModel = new FileIndexTreeViewModel();
            SelectedFileVersion = null;

            DataContext = this;
            FileIndexTreeView.DataContext = TreeViewModel;

            FileIndexTreeView.SelectedItemChanged += OnSelectedTreeItemChanged;

            Task.Run(FillFileIndexTree).Wait();
        }


        private async void OnSelectedTreeItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileIndexTreeItemViewModel treeItem && treeItem.IsValid)
            {
                await LoadFileVersions(treeItem.FileId);
            }
            else
            {
                FileVersions.Clear();
            }
        }

        private async Task LoadFileVersions(Guid fileId)
        {
            try
            {
                FileVersions.Clear();
                var api = _apiFactory.Create();
                var versions = await api.GetFileVersionInfosForFileAsync(fileId);

                foreach (var version in versions.FileVersionsInfos)
                {
                    FileVersions.InsertSorted(new FileVersionListItemViewModel
                    {
                        FileVersionId = version.FileVersionId,
                        ClientPath = Path.Combine(version.ClientDirPath ?? "", version.ClientFileName),
                        VersionNr = version.VersionNr,
                        Md5 = version.Md5,
                        SizeBytes = version.SizeBytes,
                        CreatedDate = version.CreatedDate.DateTime
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
            if (SelectedFileVersion == null) return;

            try
            {
                var api = _apiFactory.Create();
                // TODO: Call API to apply selected version
                // TODO: download that version through SyncService
                await Task.CompletedTask; // Placeholder until API method is available

                MessageBox.Show($"Przywrócono wersję {SelectedFileVersion.VersionNr}!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError("Błąd przywracania wersji: {}", ex.Message);
                MessageBox.Show($"Błąd przywracania wersji: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task FillFileIndexTree()
        {
            try
            {
                var api = _apiFactory.Create();
                SyncAllExtResponse resp = await api.SyncAllExtAsync(true);

                var fvs = resp.CurrentFileVersionsInfosExt;

                foreach (var fv in fvs)
                {
                    var clientFilePath = Path.Combine(fv.ClientDirPath ?? string.Empty, fv.ClientFileName) ?? string.Empty;
                    var treeItem = new FileIndexTreeItemViewModel(clientFilePath, fv.Deleted, fv.FileId);
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
    }
}