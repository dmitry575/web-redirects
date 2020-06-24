using System;
using System.Configuration.Abstractions;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using WebProxy.Config;
using WebProxy.Domains;
using WebProxy.Extensions;

namespace WebProxy.Services
{
    public class RequestRedirect : IRequestRedirect
    {
        /// <summary>
        /// хедер по которому определяем куда перенаправлять
        /// </summary>
        private const string HEADER_URL = "url";

        private const int BUFFER_SIZE = 64000;
        /// <summary>
        /// Настройки
        /// </summary>
        private readonly RedirectConfigurationSection _configuration;
        private readonly IHttpRequest _httpRequest;
        private static readonly ILog Log = LogManager.GetLogger(typeof(RequestRedirect));
        public RequestRedirect(IConfiguration configurationManager, IHttpRequest httpRequest)
        {
            _configuration = configurationManager.GetSection("redirect").Get<RedirectConfigurationSection>();
            _httpRequest = httpRequest;

        }

        /// <summary>
        /// Редирект данных через тор
        /// https://github.com/aspnet/Proxy
        /// </summary>

        public async Task Redirect(HttpContext context)
        {
            var request = context.Request;
            if (!request.Headers.ContainsKey(HEADER_URL))
            {
                context.Response.StatusCode = 405;
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("header url not exists"));

                Log.Error($"header port not exists, {request.GetDisplayUrl()}");
                return;
            }

            string url = request.Headers[HEADER_URL];

            if (string.IsNullOrEmpty(url))
            {
                context.Response.StatusCode = 406;
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"invalid url {url}"));

                Log.Error($"invalid port {request.Headers[HEADER_URL]}, {request.GetDisplayUrl()}");
                return;
            }

            string newUrl = GetUrlRedirect(url);

            HttpResponseMessage responseMessage = null;

            try
            {
                responseMessage = await _httpRequest.RequestAsync(newUrl, request, context.RequestAborted);
            }
            catch (Exception e)
            {
                Log.Error($"request failed to {newUrl},  {e}");
                return;
            }

            if (responseMessage == null)
            {
                Log.Error($"request failed to {newUrl}, url: {url} responseMessageis null");
                return;
            }
            try
            {
                await CopyProxyHttpResponse(context, responseMessage);
            }
            catch (Exception e)
            {
                Log.Error($"copy data failed from {newUrl} to {request.Headers[HEADER_URL]}, {e}");
            }

        }
        /// <summary>
        /// Копирование заголовков
        /// </summary>
        /// <param name="context"></param>
        /// <param name="responseMessage"></param>
        /// <returns></returns>
        public static async Task CopyProxyHttpResponse(HttpContext context, HttpResponseMessage responseMessage)
        {
            if (responseMessage == null)
            {
                throw new ArgumentNullException(nameof(responseMessage));
            }

            var response = context.Response;

            response.StatusCode = (int)responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
            response.Headers.Remove("transfer-encoding");

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                await responseStream.CopyToAsync(response.Body, BUFFER_SIZE, context.RequestAborted);
            }
        }

        /// <summary>
        /// Урл для редиректа
        /// </summary>
        /// <param name="url"></param>
        /// <param name="request"></param>
        private string GetUrlRedirect(string url)
        {
            url = url.ToUrl();
            if (url.EndsWith('/'))
            {
                url = url.Substring(0, url.Length - 1);
            }
            return url;

        }
    }
}
