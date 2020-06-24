using System;
using System.Net;
using MihaZupan;
using xNet;

namespace ProxyWork.ProxyParser
{
    /*public enum ProxyType
    {
        Http = 0,
        Socks4 = 1,
        Socks5 = 2,

    }*/

    public enum ProxyStatus
    {
        OK = 1,
        Add = 2,
        Deleted = 3
    }

    public class ProxyInfo
    {
        /// <summary>
        /// Адрес
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Порт
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Тип прокси
        /// </summary>
        public ProxyType Type { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime LastCheck { get; set; }

        public ProxyStatus Status { get; set; }

        private IWebProxy _webProxy = null;

        public override string ToString()
        {
            return $"{Address}|{Port}|{(int)Type}|{DateCreated}|{LastCheck}|{(int)Status}";
        }

        /// <summary>
        /// Ключ
        /// </summary>
        public string GetKey()
        {
            return string.Format(
                $"{Type.ToString().ToLower()}:{Address.ToLower()}:{Port}");
        }
        public bool Load(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            var list = str.Split('|');
            if (list.Length < 4)
                return false;

            Address = list[0];
            int port;
            if (!int.TryParse(list[1], out port))
                return false;

            Port = port;

            int type;
            if (!int.TryParse(list[2], out type))
                return false;

            Type = (ProxyType)type;

            DateCreated = DateTime.Parse(list[3]);
            LastCheck = DateTime.Parse(list[4]);

            if (list.Length > 4)
            {
                int status;
                if (int.TryParse(list[5], out status))
                {
                    Status = (ProxyStatus)status;
                }
                else
                {
                    Status = ProxyStatus.Add;
                }
            }

            return true;
        }

        internal static int GetPort(string port)
        {
            int p;
            if (int.TryParse(port, out p))
                return p;
            return -1;
        }

        internal static ProxyType GetType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return ProxyType.Http;
            switch (type.ToLower())
            {
                case "socks4":
                    return ProxyType.Socks4;
                case "socks5":
                    return ProxyType.Socks5;
            }
            return ProxyType.Http;
        }

        public IWebProxy GetProxy()
        {
            if (_webProxy != null)
            {
                return _webProxy;
            }
            switch (Type)
            {
                case ProxyType.Socks4:
                case ProxyType.Socks5:
                    _webProxy = new HttpToSocks5Proxy(Address, Port);
                    break;
                default:
                    _webProxy = new WebProxy(Address, Port);
                    break;
            }

            return _webProxy;
        }
    }
}
