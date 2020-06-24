using System;
using System.Net.Http;
using System.Text;
using AngleSharp.Html.Parser;
using CloudFlareUtilities;
using log4net;
using Polly;
using ProxyWork.HideMyna;
using ProxyWork.ProxyParser;
using xNet;

namespace ProxyWork.ProxyListOrg
{
    public class ProxyListOrgParser : IProxyParser
    {
        public delegate void Action<T>(T obj);

        private const string URL = "https://proxy-list.org/russian/index.php";
        private const string URL_PAGE = "https://proxy-list.org/russian/index.php?p={0}";
        
        private HttpClient _client;
        private static readonly ILog Log = LogManager.GetLogger(typeof(HidemynaParser));
        private int _countParse;
        //private List<ProxyInfo> _listProxy = new List<ProxyInfo>();

        private readonly Action<ProxyInfo> _addProxyFunc;
        public ProxyListOrgParser(Action<ProxyInfo> addFunc)
        {
            _addProxyFunc = addFunc;
        }

        public int DoParse()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding.GetEncoding("urf-8");
                _countParse = 0;
                //_listProxy.Clear();
                ConnectOverCloudFare();
                int page = GetPage();
                GetListServersAsync(page);
            }
            catch (Exception e)
            {
                Log.Error($"parsing hide menya {e}");
            }

            return _countParse;
        }

        private void GetListServersAsync(int pages)
        {
            string url = string.Empty;
            try
            {
                for (int i = 2; i <= pages; i++)
                {
                    url = i == 0 ? URL : string.Format(URL_PAGE, i );

                    var html = _client.GetStringAsync(URL).Result;

                    var parser = new HtmlParser();
                    var document = parser.ParseDocument(html);
                    var lis = document.QuerySelectorAll(".proxy__t tr");
                    foreach (var tr in lis)
                    {
                        var tds = tr.QuerySelectorAll("td");
                        if (tds == null || tds.Length < 4)
                            continue;
                        string ip = tds[0].InnerHtml;
                        int port = ProxyInfo.GetPort(tds[1].InnerHtml);
                        if (port < 0)
                        {
                            Log.Warn($"invalid port: {tds[1].InnerHtml}");
                            continue;
                        }
                        var type = ProxyInfo.GetType(tds[4].InnerHtml);

                        var newData = new ProxyInfo
                        {
                            Address = ip, DateCreated = DateTime.Now, LastCheck = DateTime.MinValue, Port = port, Type = type,
                            Status = ProxyStatus.Add
                        };

                        _addProxyFunc(newData);
                        _countParse++;
                        //_listProxy.Add();
                        Log.Info($"add new proxy: {type} {ip}:{port}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"get list servers failed: {url}, {e}");
            }

        }


        private void ConnectOverCloudFare()
        {
            var handler = new ClearanceHandler();
            handler.MaxRetries = 5;
            //handler.MaxRetries = ClearanceHandler.DefaultMaxRetries;

            _client = new HttpClient(handler);
            _client.Timeout = TimeSpan.FromMinutes(5);
        }

        private int GetPage()
        {
            var html = string.Empty;
            try
            {
                Policy.Handle<HttpException>().WaitAndRetry(
                    7,
                    retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Log.Warn($"send request to {URL} failed, retry: {retryCount}, {exception}");
                    }).Execute(
                    () =>
                    {
                        html = _client.GetStringAsync(URL).Result;
                    });
            }
            catch (AggregateException ex) when (ex.InnerException is CloudFlareClearanceException)
            {
                Log.Error(ex.InnerException.Message);
            }

            if (string.IsNullOrEmpty(html))
                return 0;
            try
            {
                int page = 1;
                var parser = new HtmlParser();
                var document = parser.ParseDocument(html);
                var lis = document.QuerySelectorAll(".proxy__pagination li");
                foreach (var li in lis)
                {
                    var tagA = li.QuerySelector("a");
                    if (tagA != null)
                    {
                        int temp;
                        if (int.TryParse(tagA.InnerHtml, out temp))
                            if (temp > page)
                                page = temp;
                    }
                }
                return page;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return 0;
        }

        public string GetUrl()
        {
            return URL;
        }
    }
}
