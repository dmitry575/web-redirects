

using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using WebProxy.Services;

namespace WebProxy.Middlewares
{
    public class WebRequestMiddleware
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WebRequestMiddleware));
        private readonly RequestDelegate _next;

        private readonly IRequestRedirect _requestRedirect;


        public WebRequestMiddleware(RequestDelegate next, IRequestRedirect requestRedirect)
        {
            _next = next;
            _requestRedirect = requestRedirect;

        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                Log.Info($"begin request: {context.Request.GetDisplayUrl()}");
               await _requestRedirect.Redirect(context);
            }
            catch (Exception e)
            {
                Log.Error($"redirect failed: {context.Request.GetDisplayUrl()}, {e}");
                await _next(context);
            }
        }
    }
}
