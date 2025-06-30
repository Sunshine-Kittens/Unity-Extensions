using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public interface IHttpResponseBody : IDisposable
    {
        private class EmptyHttpResponseBody : IHttpResponseBody
        {
            public ulong TotalBytes => 0;
            public ulong ReceivedBytes => 0;
            public float PercentComplete => 1.0F;
            public bool IsCompleted => true;
            public byte[] Data => Array.Empty<byte>();

            public ValueTask WaitForCompletionAsync(CancellationToken _)
            {
                return new ValueTask(Task.CompletedTask);
            }

            public void Dispose() { }
        }
        
        public static readonly IHttpResponseBody Empty =  new EmptyHttpResponseBody(); 
        
        ulong TotalBytes { get; }
        ulong ReceivedBytes { get; }
        float PercentComplete { get; }
        bool IsCompleted { get; }
        byte[] Data { get; }
        
        public ValueTask WaitForCompletionAsync(CancellationToken cancellationToken);
    }
}
