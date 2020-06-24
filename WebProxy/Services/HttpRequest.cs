using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MihaZupan;
using ProxyWork.ProxyChecks;
using WebProxy.Config;
using xNet;
using HttpMethod = System.Net.Http.HttpMethod;
using StreamContent = System.Net.Http.StreamContent;

namespace WebProxy.Services
{
    /// <summary>
    /// Information about proxies
    /// </summary>
    class ProxyInfo
    {
       
        /// <summary>
        /// Data of tor proxy
        /// </summary>
        public HttpToSocks5Proxy SocksWebProxy { get; private set; }

        private int _invalidRequests;
        public int InvalidRequests
        {
            get { return _invalidRequests; }
        }

        public ProxyInfo(HttpToSocks5Proxy proxy)
        {
            SocksWebProxy = proxy;
            _invalidRequests = 0;
        }
        /// <summary>
        /// Увеличение неправильных попыток
        /// </summary>
        public void IncInvalid()
        {
            Interlocked.Increment(ref _invalidRequests);
        }
    }

    public class HttpRequest : IHttpRequest, IDisposable
    {
        /// <summary>
        /// Порты с которого начнутся прослушиваться TOR
        /// </summary>
        private int m_PortsFrom;
        /// <summary>
        /// Порты с которого начнутся прослушиваться TOR
        /// </summary>
        private int m_PortsControlFrom;
        /// <summary>
        /// Порты до которого начнутся прослушиваться TOR
        /// </summary>
        private int m_PortsCount;

        private int _lastSocksProxy = -1;

        private const int MAX_INVALID_CONNECT = 5;

        private const int LockerTimeuotMs = 5;
        private readonly ReaderWriterLockSlim _lockerQuestions = new ReaderWriterLockSlim();

        private readonly List<ProxyInfo> _proxies = new List<ProxyInfo>();
        private readonly IProxyChecker _proxyChecker;

        private readonly Random _random = new Random();

        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpRequest));

        public HttpRequest(IConfiguration configurationManager, IProxyChecker proxyChecker)
        {
            var configuration = configurationManager.GetSection("tor").Get<TorConfigurationSection>();
            m_PortsFrom = configuration.PortStart;
            m_PortsControlFrom = configuration.PortControlStart;
            m_PortsCount = configuration.PortCount;

            _proxyChecker = proxyChecker;
            SetProxy();
        }

        private void SetProxy()
        {
            if (_lockerQuestions.TryEnterWriteLock(LockerTimeuotMs))
            {
                if (_proxies.Count <= 0)
                {

                    try
                    {
                        for (int i = m_PortsFrom; i < m_PortsFrom + m_PortsCount; i++)
                        {
                            try
                            {
                                var proxy = new HttpToSocks5Proxy("127.0.0.1", i);
                                _proxies.Add(new ProxyInfo(proxy));
                                Log.Info($"add proxy to list port: {i}");
                            }
                            catch (Exception e)
                            {
                                throw new Exception("proxy enable failed, try changes port_from: " + e);
                            }

                        }
                    }
                    finally
                    {
                        _lockerQuestions.ExitWriteLock();
                    }
                }

                // включение и загрузка
                //_proxyChecker.StartCheck();
            }
            Log.Info($"tor proxies loaded: {_proxies.Count}");
            Log.Info($"another proxies loaded: {_proxyChecker.GetCount()}");
        }

        /// <summary>
        /// Соглашаться на самоподписанный сертификат
        /// </summary>
        private bool OnServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        /// <summary>
        /// Случайный прокси
        /// </summary>
        /// <returns></returns>
        private IWebProxy GetProxy(out int index)
        {
            if (_proxies == null || _proxies.Count <= 0)
            {
                SetProxy();
            }

            if (_proxies == null || _proxies.Count <= 0)
            {
                index = 0;
                return null;
            }

            if (_random.Next(0, 3) > 1)
            {
                var proxy = _proxyChecker.GetNext();
                if (proxy != null)
                {
                    index = _lastSocksProxy;
                    return proxy.GetProxy();

                }
            }

            if (_lockerQuestions.TryEnterWriteLock(LockerTimeuotMs))
            {
                try
                {
                    if (_lastSocksProxy < 0)
                    {
                        _lastSocksProxy = 0;
                        index = 0;
                    }
                    else
                    {

                        if ((_lastSocksProxy + 1) >= _proxies.Count)
                        {
                            _lastSocksProxy = -1;
                            index = -1;
                            return null;
                        }
                        else
                        {
                            _lastSocksProxy++;
                        }
                    }

                    index = _lastSocksProxy;
                    return _proxies[_lastSocksProxy].SocksWebProxy;
                }
                finally
                {
                    _lockerQuestions.ExitWriteLock();
                }
            }
            index = 0;
            return null;
        }


        public async Task<HttpResponseMessage> RequestAsync(string url, Microsoft.AspNetCore.Http.HttpRequest request,
            CancellationToken cancellationToken)
        {
            var requestMessage = CreateProxyHttpRequest(request, new Uri(url));
            int index;

            var proxy = GetProxy(out index);
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                Proxy = proxy
            };

            using (var client = new HttpClient(handler))
            {
                try
                {
                    Log.Info($"request to {url}, proxy id: {index}, {proxy}");
                    HttpResponseMessage res = await client.SendAsync(requestMessage,
                        HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    return res;
                }
                catch (HttpException e)
                {
                    //UpdateInvalid(proxy.Credentials.GetCredential());
                    Log.Error($"HttpExeption: request failed {url}, proxy id: {index}, {proxy}, {e.Message}");
                    return null;
                }
                catch (SocketException e)
                {
                    //UpdateInvalid(proxy.Credentials.GetCredential());
                    Log.Error($"SocketException: request failed {url}, proxy id: {index}, {proxy}, {e.Message}");
                    return null;
                }
                catch (Exception e)
                {
                    //UpdateInvalid(proxy.Credentials.GetCredential());
                    Log.Error($"Exception: request failed {url}, proxy id: {index}, {proxy}, {e.Message}");
                    return null;
                }
                finally
                {
                    Log.Info($"request finished {url}, proxy id: {index}, {proxy}");

                }

            }
        }

        private void UpdateInvalid(HttpToSocks5Proxy proxy)
        {
            if (_lockerQuestions.TryEnterWriteLock(LockerTimeuotMs))
            {
                try
                {
                    var obj = _proxies.FirstOrDefault(x => x.SocksWebProxy == proxy);
                    if (obj != null)
                    {
                        obj.IncInvalid();
                        if (obj.InvalidRequests > MAX_INVALID_CONNECT)
                        {
                            _proxies.Remove(obj);
                            Log.Info($"delete proxy from list: {proxy}, invalids: {obj.InvalidRequests}");
                        }
                    }

                }
                finally
                {
                    _lockerQuestions.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Генерация заголовков
        /// </summary>
        /// <param name="request"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static HttpRequestMessage CreateProxyHttpRequest(Microsoft.AspNetCore.Http.HttpRequest request, Uri uri)
        {

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in request.Headers)
            {
                if (string.Compare(header.Key, "port", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
        ~HttpRequest()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (_proxies != null && _proxies.Count > 0)
            {
                foreach (var proxy in _proxies)
                {
                    //proxy.SocksWebProxy;
                }
            }
            _proxyChecker.Stop();
        }
    }
}
