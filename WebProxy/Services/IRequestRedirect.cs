using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebProxy.Services
{
    public interface IRequestRedirect
    {
        Task Redirect(HttpContext context);
    }
}
