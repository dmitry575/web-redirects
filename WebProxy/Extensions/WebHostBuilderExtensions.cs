using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebProxy.Extensions
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder ConfigureApplicationLogging(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureLogging((context, builder) =>
            {
                builder.ClearProviders();
                builder.AddConfiguration(context.Configuration);
                builder.AddLog4Net();

                if (context.HostingEnvironment.IsDevelopment())
                {
                    builder.AddConsole();
                }
            });
        }
    }
}