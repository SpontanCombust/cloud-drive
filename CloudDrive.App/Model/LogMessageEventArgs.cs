using Microsoft.Extensions.Logging;

namespace CloudDrive.App.Model
{
    public class LogMessageEventArgs : EventArgs
    {
        public required string CategoryName { get; set; }
        public LogLevel Level { get; set; }
        public required string Message { get; set; }
    }
}
