using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using WebProxy.Extensions;

namespace WebProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("ASPNETCORE_");
                .Build();

            CreateWebHostBuilder(args, config).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, IConfigurationRoot config) =>
            WebHost
                .CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                    
                })
                .PreferHostingUrls(true)
                .ConfigureApplicationLogging()
                .UseStartup<Startup>()
                .UseUrls(config["server:url"]);
    }
}
