using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using ProxyWork.HideMyna;
using ProxyWork.ProxyParser;
using xNet;

namespace ProxyWork.ProxyChecks
{
    public class ProxyChecker : IProxyChecker, IDisposable
    {
        private const int LOCK_TIMEOUT = 5000;
        private const int MAX_THREADS = 10;
        private const string URL_CHECK = "https://www.microsoft.com/ru-ru/";
        private readonly SortedDictionary<string, ProxyInfo> _proxies = new SortedDictionary<string, ProxyInfo>();
        private readonly DataProxies _dataProxies;
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _lockerProgress = new ReaderWriterLockSlim();

        private static readonly ILog Log = LogManager.GetLogger(typeof(ProxyChecker));

        private readonly ReaderWriterLockSlim _lockerProxyList = new ReaderWriterLockSlim();
        private List<ProxyInfo> _readyProxiesList = new List<ProxyInfo>();
        private readonly List<Task> _tasksParsers = new List<Task>();
        private int _lastProxyId = -1;

        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        private Task _taskCheck = null;

        private bool _inProgress = false;

        public ProxyChecker(string fileProxies = "./proxies.dat")
        {
            _dataProxies = new DataProxies(fileProxies);
        }

        private void StartParse()
        {
            _tasksParsers.Add(Task.Run(() =>
            {
                HidemynaParser.Action<ProxyInfo> action = AddProxy;

                HidemynaParser hidemynaParser = new HidemynaParser(action);
                var count = hidemynaParser.DoParse();
                Log.Info($"hide my parse: {count}");
            }));
        }

        /// <summary>
        /// Добавление единичного
        /// </summary>
        /// <param name="proxyInfo"></param>
        private void AddToReadyList(ProxyInfo proxyInfo)
        {
            try
            {
                if (_lockerProxyList.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    _readyProxiesList.Add(proxyInfo);
                }
            }
            catch (Exception e)
            {
                Log.Error($"add proxy ready failed, {proxyInfo}: {e}");
            }
            finally
            {
                if (_lockerProxyList.IsWriteLockHeld)
                    _lockerProxyList.ExitWriteLock();
            }
        }


        /// <summary>
        /// Удаление единичного
        /// </summary>
        /// <param name="proxyInfo"></param>
        private void DeleteReadyList(ProxyInfo proxyInfo)
        {
            try
            {
                if (_lockerProxyList.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    _readyProxiesList.Remove(proxyInfo);
                }
            }
            catch (Exception e)
            {
                Log.Error($"remove proxy ready failed, {proxyInfo}: {e}");
            }
            finally
            {
                if (_lockerProxyList.IsWriteLockHeld)
                    _lockerProxyList.ExitWriteLock();
            }
        }
        private void LoadReady()
        {
            try
            {
                if (_lockerProxyList.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    _readyProxiesList = _proxies.Where(x => x.Value.Status == ProxyStatus.OK).Select(x => x.Value)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Log.Error($"get list of proxies faield: {e}");
            }
            finally
            {
                if (_lockerProxyList.IsWriteLockHeld)
                    _lockerProxyList.ExitWriteLock();
            }
        }

        /// <summary>
        /// Следующий прокси
        /// </summary>
        /// <returns></returns>
        public ProxyInfo GetNext()
        {
            try
            {
                if (_lockerProxyList.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    if (_readyProxiesList.Count <= 0)
                        return null;
                    if (_lastProxyId < 0)
                        _lastProxyId = 0;
                    if (_lastProxyId >= _readyProxiesList.Count)
                        _lastProxyId = 0;
                    Log.Info($"get next proxy checker info: {_lastProxyId}");
                    var next = _readyProxiesList[_lastProxyId];

                    Log.Info($"get next proxy checker info: {next}");

                    _lastProxyId++;
                    return next;
                }
            }
            catch (Exception e)
            {
                Log.Error($"get next proxy failed: {_lastProxyId} {e}");
            }
            finally
            {
                if (_lockerProxyList.IsWriteLockHeld)
                    _lockerProxyList.ExitWriteLock();
            }

            return null;
        }

        public int GetCount()
        {
            return _readyProxiesList.Count;
        }

        public Task StartCheck()
        {
            _taskCheck = Task.Run(Checks);
            return _taskCheck;
        }

        /// <summary>
        /// Checks proxies
        /// </summary>
        private  void Checks()
        {
            AddProxies(_dataProxies.Load());

            LoadReady();

            StartParse();

            if (!LockCheck(true))
            {
                return;
            }

            List<ProxyInfo> proxies = null;
            try
            {
                proxies = _proxies.Where(x =>
                    x.Value.Status == ProxyStatus.Add || (x.Value.LastCheck <= DateTime.UtcNow.AddHours(-1) && x.Value.Status == ProxyStatus.OK))
                    .Select(x => x.Value).ToList();

            }
            catch (Exception e)
            {
                Log.Error($"get list of proxies faield: {e}");
                return;
            }
            finally
            {
                ExitCheck();
            }

            if (proxies != null)
            {
                var tasks = new List<Task>();

                foreach (var proxyInfo in proxies)
                {

                    if (tasks.Count > MAX_THREADS)
                    {
                        var idx = Task.WaitAny(tasks.ToArray());
                        tasks.RemoveAt(idx);
                    }

                    tasks.Add(Task.Run(() =>
                    {
                        Log.Info($"checking proxy => {proxyInfo}");
                        if (GetRequest(proxyInfo))
                        {
                            Log.Info($"check proxy => {proxyInfo.Address} {proxyInfo.Type} is OK");
                            UpdateStatus(proxyInfo.GetKey(), ProxyStatus.OK, DateTime.UtcNow);
                            AddToReadyList(proxyInfo);
                        }
                        else
                        {
                            Log.Info($"check proxy => {proxyInfo.Address} {proxyInfo.Type} is Deleted");
                            UpdateStatus(proxyInfo.GetKey(), ProxyStatus.Deleted, DateTime.UtcNow);
                            DeleteReadyList(proxyInfo);
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray(), _cancellationToken.Token);
                SaveProxies();
            }
        }

        private bool GetRequest(ProxyInfo proxy)
        {
            try
            {
                using (HttpRequest client = GenerateHttpClientWithProxySettings(proxy))
                {
                    client.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    client.AddHeader("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:56.0) Gecko/20100101 Firefox/56.0");
                    client.AddHeader("Accept-Language", "en-US;q=0.7,en;q=0.3");
                    //client["Accept-Encoding"]= "gzip, deflate, br";
                    client.EnableEncodingContent = true;
                    client.AddHeader("Referer", URL_CHECK);

                    client.SslCertificateValidatorCallback = (sender, certificate, chain, errors) => { return true; };
                    var message = client.Get(URL_CHECK);
                    return message.IsOK;
                }
            }
            catch (Exception e)
            {
                Log.Error($"error request via proxy: {proxy}, {e}");
            }

            return false;
        }

        private HttpRequest GenerateHttpClientWithProxySettings(ProxyInfo proxy)
        {
            var request = new HttpRequest();
            request.Proxy = GetProxyClient(proxy);
            request.AllowAutoRedirect = true;
            request.ConnectTimeout = 60 * 1000;
            return request;
        }

        /// <summary>
        /// Получение прокси
        /// </summary>
        /// <param name="proxy"></param>
        private ProxyClient GetProxyClient(ProxyInfo proxy)
        {
            switch (proxy.Type)
            {
                case ProxyType.Socks4:
                    return new Socks4ProxyClient(proxy.Address, proxy.Port) { ConnectTimeout = 60 * 1000 };
                case ProxyType.Socks5:
                    return new Socks5ProxyClient(proxy.Address, proxy.Port) { ConnectTimeout = 60 * 1000 };

                default:
                    return new HttpProxyClient(proxy.Address, proxy.Port) { ConnectTimeout = 60 * 1000 };
            }
        }


        private void ExitCheck()
        {
            LockCheck(false);
        }

        private bool LockCheck(bool state)
        {
            try
            {
                if (_lockerProgress.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    if (!_inProgress)
                    {
                        _inProgress = state;
                        return true;
                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (_lockerProgress.IsWriteLockHeld)
                    _lockerProgress.ExitWriteLock();
            }
            return false;
        }

        private void AddProxies(List<ProxyInfo> proxies)
        {
            try
            {
                if (_locker.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    foreach (var proxy in proxies)
                    {
                        Add(proxy);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (_locker.IsWriteLockHeld)
                {
                    _locker.ExitWriteLock();
                }
            }
        }

        private void Add(ProxyInfo proxyInfo)
        {
            var key = proxyInfo.GetKey();
            if (!_proxies.ContainsKey(key))
            {
                _proxies.Add(key, proxyInfo);
            }
        }

        /// <summary>
        /// Добавление прокси
        /// </summary>
        /// <param name="proxyInfo"></param>
        public void AddProxy(ProxyInfo proxyInfo)
        {
            try
            {
                if (_locker.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    Add(proxyInfo);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (_locker.IsWriteLockHeld)
                {
                    _locker.ExitWriteLock();
                }
            }
            if (_proxies.Count % 10 == 0)
            {
                SaveProxies();
            }
        }

        /// <summary>
        /// Добавление прокси
        /// </summary>
        /// <param name="key"></param>
        /// <param name="status"></param>
        /// <param name="dateCheck"></param>
        public void UpdateStatus(string key, ProxyStatus status, DateTime dateCheck)
        {
            try
            {
                if (_locker.TryEnterWriteLock(LOCK_TIMEOUT))
                {
                    if (_proxies.ContainsKey(key))
                    {
                        var proxyInfo = _proxies[key];
                        proxyInfo.Status = status;
                        proxyInfo.LastCheck = dateCheck;
                        _proxies[key] = proxyInfo;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (_locker.IsWriteLockHeld)
                {
                    _locker.ExitWriteLock();
                }
            }
        }

        public void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            SaveProxies();
            try
            {
                _taskCheck?.Dispose();
                foreach (var task in _tasksParsers)
                {
                    task.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.Error($"task proxy check stop failed: {e}");
            }

        }

        private void SaveProxies()
        {
            try
            {
                if (_locker.TryEnterReadLock(LOCK_TIMEOUT))
                {
                    _dataProxies.Save(_proxies.Where(x => x.Value.Status != ProxyStatus.Deleted).Select(x => x.Value).ToList());
                }
                else
                {
                    Log.Warn("Failed saved proxies, to file");
                }
            }
            catch (Exception e)
            {
                Log.Error($"save proxies failed, {e}");
            }
            finally
            {
                if (_locker.IsReadLockHeld)
                {
                    _locker.ExitReadLock();
                }
            }
        }
    }
}
