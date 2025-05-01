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

        public StatusPage(ILogRelayService logRelay, ILogHistoryService logHistory, ISyncService syncService)
        {
            _logRelay = logRelay;
            _logHistory = logHistory;
            _syncService = syncService;

            InitializeComponent();

            logRelay.LogAdded += onLogAdded;
            foreach(var e in logHistory.GetHistory() ) {
                LogTextBox.Text += e.Message + Environment.NewLine;
            }
        }

        private void onLogAdded(object? sender, LogMessageEventArgs e)
        {
            LogTextBox.Text += e.Message + Environment.NewLine;
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
    }
}
