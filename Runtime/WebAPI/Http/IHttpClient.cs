using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public interface IHttpClient
    {
        public ValueTask<IHttpResponse> SendAsync(HttpRequest request, CancellationToken cancellationToken = default);
        public ValueTask<IHttpResponse> ScheduleAsync(HttpRequest request, float seconds, CancellationToken cancellationToken = default);
    }
}