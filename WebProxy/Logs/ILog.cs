using System;

namespace WebProxy.Logs
{
    public interface ILog
    {
        bool IsDebugEnabled { get; }

        void Debug(string message, params object[] args);

        void Info(string message, params object[] args);

        void Warning(string message, params object[] args);

        void Error(string message, params object[] args);

        void Error(Exception exception, string message, params object[] args);

        void Fatal(string message, params object[] args);

        void Fatal(Exception exception, string message, params object[] args);
    }
}
