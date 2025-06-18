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

        public readonly StatusPageViewModel ViewModel;

        public StatusPage(
            ILogRelayService logRelay,
            ILogHistoryService logHistory,
            ISyncService syncService,
            IViewLocator viewLocator,
            IFileSystemWatcher fileSystemWatcher,
            IBenchmarkService benchmarkService)
        {
            _logRelay = logRelay;
            _logHistory = logHistory;
            _syncService = syncService;
            _viewLocator = viewLocator;
            _fileSystemWatcher = fileSystemWatcher;
            _benchmarkService = benchmarkService;

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
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nie udało się uruchomić obserwatora: " + ex.Message);
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

        private async void FullSyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.SyncIsInProgress = true;
                await _syncService.SynchronizeAllFilesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd w synchronizacji: " + ex.Message);
            }
            finally
            {
                ViewModel.SyncIsInProgress = false;
            }
        }

        private void logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _fileSystemWatcher.Stop();
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
