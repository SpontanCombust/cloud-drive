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
using CloudDrive.App.Model;
using CloudDrive.App.Services;
using CloudDrive.App.ServicesImpl;



namespace CloudDrive.App.Views
{
    public partial class StatusPage : Page
    {
        private readonly ILogRelayService _logRelay;
        private readonly ILogHistoryService _logHistory;
        private readonly ISyncService _syncService;
        private readonly IViewLocator _viewLocator;
        private readonly IFileSystemWatcher _fileSystemWatcher;

        public StatusPage(ILogRelayService logRelay, ILogHistoryService logHistory, ISyncService syncService, IViewLocator viewLocator, IFileSystemWatcher fileSystemWatcher)
        {
            _logRelay = logRelay;
            _logHistory = logHistory;
            _syncService = syncService;
            _viewLocator = viewLocator;
            _fileSystemWatcher = fileSystemWatcher;

            InitializeComponent();

            logRelay.LogAdded += onLogAdded;
            foreach(var e in logHistory.GetHistory() ) {
                LogTextBox.Text += e.Message + Environment.NewLine;
            }

            try
            {
                _fileSystemWatcher.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się uruchomić obserwatora: " + ex.Message);
            }
        }

        private void onLogAdded(object? sender, LogMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.Text += e.Message + Environment.NewLine;
            });
        }

        private void FullSyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _syncService.SynchronizeAllFilesAsync();
            }
            catch (Exception ex) {
                MessageBox.Show("Błąd w synchronizacji: " + ex.Message);
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
    }
}
