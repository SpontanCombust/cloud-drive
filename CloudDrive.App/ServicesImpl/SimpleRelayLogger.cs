using CloudDrive.App.Model;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CloudDrive.App.ServicesImpl
{
    internal class SimpleRelayLogger : ILogger
    {
        private readonly ILogRelayService _relayService;
        private readonly string _categoryName;

        public SimpleRelayLogger(ILogRelayService relayService, string categoryName)
        {
            _relayService = relayService;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var message = new StringBuilder($"[{timestamp}] ");
            message.Append(formatter(state, exception));

            if (exception != null)
            {
                message.AppendLine();
                message.AppendLine(exception.ToString());
            }

            var ev = new LogMessageEventArgs
            {
                CategoryName = _categoryName,
                Level = logLevel,
                Message = message.ToString(),
                Exception = exception
            };

            _relayService.Relay(ev);
        }
    }
}
