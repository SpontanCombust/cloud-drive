using CloudDrive.App.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CloudDrive.App.ServicesImpl
{
    public sealed class SimpleRelayLoggerProvider : ILoggerProvider
    {
        private readonly ILogRelayService _relayService;

        private readonly ConcurrentDictionary<string, SimpleRelayLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

        public SimpleRelayLoggerProvider(ILogRelayService logRelay)
        {
            _relayService = logRelay;
        }


        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, cn => new SimpleRelayLogger(_relayService, cn));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
