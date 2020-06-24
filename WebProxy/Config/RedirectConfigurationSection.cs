using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Abstractions;

namespace WebProxy.Config
{
    public class RedirectConfigurationSection
    {
        public static RedirectConfigurationSection GetSection(IConfigurationManager configurationManager)
        {
            return configurationManager.GetSection<RedirectConfigurationSection>("redirect");
        }

        /// <summary>
        /// Список урлов для редиректа
        /// </summary>
        public List<UrlRedirectConfig> Urls { get; set; }
    }
}
