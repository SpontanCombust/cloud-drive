using CloudDrive.App.Factories;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CloudDrive.App.Views.FileHistory
{
    /// <summary>
    /// Interaction logic for FileHistoryWindow.xaml
    /// </summary>
    public partial class FileHistoryWindow : Window
    {
        private readonly WebAPIClientFactory _apiFactory;
        private readonly ILogger<FileHistoryWindow> _logger;

        public FileIndexTreeViewModel ViewModel { get; } = new();
        
        public FileHistoryWindow(WebAPIClientFactory apiFactory, ILogger<FileHistoryWindow> logger, IUserSettingsService userSettings)
        {
            _apiFactory = apiFactory;
            _logger = logger;

            InitializeComponent();
            FileIndexTreeView.DataContext = ViewModel;

            Task.Run(FillFileIndexTree).Wait();
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
                    ViewModel.InsertIndex(treeItem);
                }

                if (ViewModel.Active.Count == 0)
                {
                    
                }
            } 
            catch (Exception ex)
            {
                _logger.LogError("Błąd pobrania historii plików: {}", ex.Message);
                return;
            }
        }
    }
}
