using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ProxyWork.HideMyna;
using ProxyWork.ProxyChecks;
using ProxyWork.ProxyParser;

namespace ProxyWork
{
    

    class Program
    {

        
        private static string ClearAfterTranslate(string dirty)
        {
            string clean = dirty;
            Regex regex = new Regex(@"\[\s*([0-9]+)\s*\]");
            Regex regex2 = new Regex(@"\[\s*\/\s*([0-9]+)\s*\]");
            if (regex.IsMatch(clean))
            {
                clean = regex.Replace(clean, "[$1]");
            }
            if (regex2.IsMatch(clean))
            {
                clean = regex2.Replace(clean, "[/$1]");
            }

            return clean;
        }
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString());

            using (var proxyChecker = new ProxyChecker())
            {
                var task = proxyChecker.StartCheck();

                HidemynaParser.Action<ProxyInfo> action = proxyChecker.AddProxy;

                HidemynaParser hidemynaParser = new HidemynaParser(action);
                var count = hidemynaParser.DoParse();

                //Console.WriteLine($"parsing: {count}");
                
                Task.WaitAll(task);
            }
            
            /*Regex regex = new Regex(@"\[\s*([0-9]+)\s*\]");
            string[] checks = new[] {"[1234]", "[ 1234]", "[ 1234 ]","[ 1234   ]", "[ s1234 ]" };

            foreach (var check in checks)
            {
                    Console.WriteLine(ClearAfterTranslate(check));
                
            }
            string[] checks2 = new[] { "[/1234]", "[ / 1234]", "[   /     1234 ]", "[ /1234   ]", "[ /s1234 ]", "[/      1234            ]" };
            Regex regex2 = new Regex(@"\[\s*\/\s*([0-9]+)\s*\]");
            Console.WriteLine("================");
            foreach (var check in checks2)
            {
                Console.WriteLine(ClearAfterTranslate(check));
/*                if (regex2.IsMatch(check))
                {
                    Console.WriteLine("OK");
                    var s = regex2.Replace(check, "[/$1]");
                    Console.WriteLine(s);
                }
                else
                    Console.WriteLine("Failed");

            }*/
        }
    }
}
