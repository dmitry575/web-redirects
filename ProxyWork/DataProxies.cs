using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProxyWork.ProxyParser;

namespace ProxyWork
{
    public class DataProxies
    {
        private readonly string _fileName;

        public DataProxies(string filename)
        {
            _fileName = filename;
        }

        public void Save(List<ProxyInfo> list)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var proxyInfo in list)
            {
                sb.AppendFormat($"{proxyInfo}\r\n");
            }
            File.AppendAllText(_fileName, sb.ToString());
            sb.Clear();
        }

        public List<ProxyInfo> Load()
        {
            List<ProxyInfo> result = new List<ProxyInfo>();

            if (!File.Exists(_fileName))
                return result;
            string data = File.ReadAllText(_fileName);

            string[] split = new[] { "\r\n" };
            string[] list = data.Split(split, StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in list)
            {
                try
                {
                    var proxy = new ProxyInfo();
                    if (proxy.Load(s))
                        result.Add(proxy);
                }
                catch (Exception) { }
            }

            return result;
        }
    }
}
