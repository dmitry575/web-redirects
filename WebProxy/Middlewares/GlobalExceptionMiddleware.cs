using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace WebProxy.Middlewares
{
    /// <summary>
    /// Слой для перехватат необработанных исключений
    /// </summary>
    public class GlobalExceptionMiddleware 
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GlobalExceptionMiddleware));
        private readonly RequestDelegate _next;
        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;

        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                var msg = string.Format("While request {0} is processed the following exception is occurred", context.Request.GetDisplayUrl());
                log.Error(msg, ex);
            }
        }
    }
}