using System.Threading.Tasks;
using ProxyWork.ProxyParser;

namespace ProxyWork.ProxyChecks
{
    public interface IProxyChecker
    {
        Task StartCheck();
        void Stop();

        /// <summary>
        /// Следующий прокси
        /// </summary>
        ProxyInfo GetNext();

        /// <summary>
        /// Количество готовых прокси
        /// </summary>
        int GetCount();
    }
}
