using System;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public class HttpException : Exception
    {
        public HttpException() { }
        public HttpException(string message) : base(message) { }
        public HttpException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class HttpRequestException : HttpException
    {
        public ReadOnlyHttpResponse Response { get; }

        public HttpRequestException(HttpResponse response)
        {
            Response = new ReadOnlyHttpResponse(response);
        }

        public HttpRequestException(HttpResponse response, string message) : base(message)
        {
            Response = new ReadOnlyHttpResponse(response);
        }

        public HttpRequestException(HttpResponse response, string message, Exception inner) : base(message, inner)
        {
            Response = new ReadOnlyHttpResponse(response);
        }
    }

    public interface IHttpClient
    {
        public Task<ReadOnlyHttpResponse> Send(HttpRequest request);
        public Task<ReadOnlyHttpResponse> Schedule(HttpRequest request, float seconds);
    }
}