using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Http;
using WebProxy.Extensions;
using WebProxy.Ioc;
using WebProxy.Middlewares;

namespace WebProxy
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _hostingEnvironment;

        public Startup(IHostingEnvironment env)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = configurationBuilder.Build();
            _hostingEnvironment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.ConfigureContainer(_configuration, _hostingEnvironment);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseRightEnvironment(env, loggerFactory)

                .UseMiddleware<WebRequestMiddleware>()
                .HideServerHeaders()
                .UseStatusCodePages();

            //
            app.Run(async (context) =>
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("404");
            });
        }
    }
}
