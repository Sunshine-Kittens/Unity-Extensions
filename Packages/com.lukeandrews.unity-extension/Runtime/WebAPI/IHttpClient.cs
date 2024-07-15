using System;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public class HttpRequestException : Exception
    {
        public HttpResponse Response { get; }

        public HttpRequestException(HttpResponse response)
        {
            Response = response;
        }

        public HttpRequestException(HttpResponse response, string message) : base(message)
        {
            Response = response;
        }

        public HttpRequestException(HttpResponse response, string message, Exception inner) : base(message, inner)
        {
            Response = response;
        }
    }

    public interface IHttpClient
    {
        public Task<ReadOnlyHttpResponse> Send(HttpRequest request);
        public Task<ReadOnlyHttpResponse> Schedule(HttpRequest request, float seconds);
    }
}