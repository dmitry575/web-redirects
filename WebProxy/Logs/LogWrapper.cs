using System;
using Microsoft.Extensions.Logging;

namespace WebProxy.Logs
{
    public class LogWrapper : ILog
    {
        private readonly ILogger _logger;

        public LogWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public void Debug(string message, params object[] args)
        {
            if (IsDebugEnabled)
            {
                _logger.LogDebug(message, args);
            }
        }

        public void Info(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void Warning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void Error(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void Fatal(string message, params object[] args)
        {
            _logger.LogCritical(message, args);
        }

        public void Fatal(Exception exception, string message, params object[] args)
        {
            _logger.LogCritical(exception, message, args);
        }

        public bool IsDebugEnabled { get; }
    }
}
