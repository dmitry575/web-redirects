using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebProxy.Extensions
{
    public static class EnvironmentExtensions
    {
        public static IApplicationBuilder UseRightEnvironment(this IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            return app;
        }
        public static IApplicationBuilder HideServerHeaders(this IApplicationBuilder app)
        {
            app.Use((context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Remove("X-Frame-Options");
                    context.Response.Headers.Remove("Server");
                    context.Response.Headers.Remove("X-Powered-By");
                    context.Response.Headers.Remove("X-SourceFiles");
                    context.Response.Headers.Remove("X-AspNet-Version");
                    context.Response.Headers.Remove("X-AspNetMvc-Version");
                    return Task.CompletedTask;
                });

                return next();
            });

            return app;
        }
    }
}
