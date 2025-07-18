using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public interface IHttpRequestHandle : IDisposable
    {
        ReadOnlyHttpRequest Request { get; }
        IHttpResponse Response { get; }

        public event Action<IHttpResponse> ResponseReceived;
        public event Action<IHttpResponse> BodyReceived;
        
        public ValueTask<IHttpResponse> WaitForCompletionAsync(CancellationToken cancellationToken);
    }
}
