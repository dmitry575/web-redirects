
using System.Configuration;

namespace WebProxy.Config
{
    /// <summary>
    /// Настройки урла для редиректа
    /// </summary>
    public class UrlRedirectConfig
    {

        /// <summary>
        /// Какой порт будет передан
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// урл куда будем перенаправлять
        /// </summary>
        public string Url { get; set; }


    }
}
