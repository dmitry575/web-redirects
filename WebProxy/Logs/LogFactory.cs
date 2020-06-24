using System;
using Microsoft.Extensions.Logging;

namespace WebProxy.Logs
{
    public class LogFactory : ILogFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public LogFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public ILog Create(string name)
        {
            return new LogWrapper(_loggerFactory.CreateLogger(name));
        }

        public ILog Create(Type type)
        {
            return Create(nameof(type));
        }
    }
}
