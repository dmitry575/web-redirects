using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyWork.ProxyParser
{
    public interface IProxyParser
    {
        int DoParse();
        string GetUrl();
    }
}
