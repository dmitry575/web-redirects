using System;

namespace WebProxy.Logs
{
    public interface ILogFactory
    {
        ILog Create(string name);

        ILog Create(Type type);
    }
}
