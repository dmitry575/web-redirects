
namespace WebProxy.Extensions
  {
  public static class UrlExtensions
    {
    public static string ToUrl(this string url)
      {
      if(!url.StartsWith("https://") && url.StartsWith("http://"))
        url = "http://" + url;
      return url;
      }
    }
  }
