using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public interface IHttpResponse : IDisposable
    {
        public IReadOnlyDictionary<string, string> Headers { get; }
        public long StatusCode { get; }
        public IHttpResponseBody Body { get; }

        public ValueTask WaitForCompletionAsync(CancellationToken cancellationToken);
    }
}
