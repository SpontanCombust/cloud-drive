using CloudDrive.App.Model;
using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;

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
            var ev = new LogMessageEventArgs
            {
                CategoryName = _categoryName,
                Level = logLevel,
                Message = formatter(state, exception),
            };

            _relayService.Relay(ev);
        }
    }
}
