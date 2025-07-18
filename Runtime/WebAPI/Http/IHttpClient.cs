using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public interface IHttpClient
    {
        public ValueTask<IHttpResponse> Send(HttpRequest request, CancellationToken cancellationToken = default);
        public ValueTask<IHttpResponse> Schedule(HttpRequest request, float seconds, CancellationToken cancellationToken = default);
    }
}