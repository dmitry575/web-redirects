using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyWork.ProxyChecks;
using System;
using WebProxy.Logs;
using WebProxy.Services;
using WebProxy.Utilites;

namespace WebProxy.Ioc
{
    public static class ContainerConfig
    {
        public static IServiceCollection ConfigureContainer(this IServiceCollection services,
            IConfiguration configuration, IHostingEnvironment environment)
        {
            return services
                .AddTransient(typeof(Lazy<>), typeof(Lazier<>))
                .AddSingleton<ILogFactory, LogFactory>()
                .AddRedirectConfig()
                .AddSingleton<IProxyChecker, ProxyChecker>();
        }
        /// <summary>
        /// Settings for redirects
        /// </summary>
        private static IServiceCollection AddRedirectConfig(this IServiceCollection services)
        {
            services.AddSingleton<IRequestRedirect, RequestRedirect>()
                .AddSingleton<IHttpRequest, HttpRequest>();
            return services;
        }
    }
}
