
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebProxy.Services
{
    public interface IHttpRequest
    {
        Task<HttpResponseMessage> RequestAsync(string url, Microsoft.AspNetCore.Http.HttpRequest request, CancellationToken cancellationToken);
    }
}
