using CloudDrive.App.Model;
using CloudDrive.App.Services;
using CloudDrive.App.ServicesImpl;
using CloudDrive.App.Utils;
using CloudDrive.App.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace CloudDrive.App.Views
{
    public partial class StatusPage : Page
    {
        private readonly ILogRelayService _logRelay;
        private readonly ILogHistoryService _logHistory;
        private readonly ISyncService _syncService;
        private readonly IViewLocator _viewLocator;
        private readonly IFileSystemWatcher _fileSystemWatcher;
        private readonly IBenchmarkService _benchmarkService;
        private readonly IAutoSyncService _autoSyncService;

        public readonly StatusPageViewModel ViewModel;

        public StatusPage(
            ILogRelayService logRelay,
            ILogHistoryService logHistory,
            ISyncService syncService,
            IViewLocator viewLocator,
            IFileSystemWatcher fileSystemWatcher,
            IBenchmarkService benchmarkService,
            IAutoSyncService autoSyncService)
        {
            _logRelay = logRelay;
            _logHistory = logHistory;
            _syncService = syncService;
            _viewLocator = viewLocator;
            _fileSystemWatcher = fileSystemWatcher;
            _benchmarkService = benchmarkService;
            _autoSyncService = autoSyncService;

            ViewModel = new StatusPageViewModel();
            DataContext = ViewModel;

            InitializeComponent();

            logRelay.LogAdded += onLogAdded;
            foreach (var e in logHistory.GetHistory())
            {
                if (e.Level >= LogLevel.Information || ViewModel.DebugLogsEnabled)
                {
                    ViewModel.Logs += e.Message + Environment.NewLine;
                }
            }

            _syncService.IsBusyChanged += OnSyncServiceBusyStatusChanged;

            Task.Run(async () =>
            {
                // daj czas na pokazanie okna
                await Task.Delay(500);

                try
                {
                    await _syncService.SynchronizeAllFilesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd synchronizacji wstępnej: " + ex.Message);
                }

                try
                {
                    _fileSystemWatcher.Start();
                    _autoSyncService.StartSync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nie udało się uruchomić obserwatora lub auto-sync: " + ex.Message);
                }
            });
        }

        private void onLogAdded(object? sender, LogMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Level >= LogLevel.Information || ViewModel.DebugLogsEnabled)
                {
                    ViewModel.Logs += e.Message + Environment.NewLine;
                }
            });
        }

        private void OnSyncServiceBusyStatusChanged(object? sender, bool isSyncServiceBusy)
        {
            ViewModel.SyncIsInProgress = isSyncServiceBusy;
        }

        private async void FullSyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _syncService.SynchronizeAllFilesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd w synchronizacji: " + ex.Message);
            }
        }

        private void logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _fileSystemWatcher.Stop();
                _autoSyncService.StopSync();
                _syncService.IsBusyChanged -= OnSyncServiceBusyStatusChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zatrzymywania obserwatora: " + ex.Message);
            }

            var loginPage = _viewLocator.LoginPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(loginPage);
            }
            else
            {
                Application.Current.MainWindow.Content = loginPage;
            }
        }

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Logs = string.Empty; 
        }

        private void ViewBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _benchmarkService.OpenBenchmarkFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd otwarcia pliku: " + ex.Message);
            }
        }

        private async void FileVersionHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var fileHistoryWindow = _viewLocator.FileHistoryWindow();
            await fileHistoryWindow.FillFileIndexTree();
            fileHistoryWindow.Show();
        }
    }
}
