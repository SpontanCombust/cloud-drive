using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace CloudDrive.App.ViewModels
{
    public partial class StatusPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string logs;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LogsTextWrapping))]
        private bool logsTextWrappingEnabled; 

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AllowManualFullSync))]
        [NotifyPropertyChangedFor(nameof(SyncInProgressSpinnerVisibility))]
        private bool syncIsInProgress;

        [ObservableProperty]
        private bool debugLogsEnabled;
         

        public TextWrapping LogsTextWrapping =>
            LogsTextWrappingEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;

        public bool AllowManualFullSync => !SyncIsInProgress;

        public Visibility SyncInProgressSpinnerVisibility =>
            SyncIsInProgress ? Visibility.Visible : Visibility.Collapsed;


        public StatusPageViewModel()
        {
            logs = string.Empty;
            logsTextWrappingEnabled = true;
            syncIsInProgress = false;
            debugLogsEnabled = false;
        }
    }
}
