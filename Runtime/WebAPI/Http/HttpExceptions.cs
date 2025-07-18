using System;

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
        public HttpRequestError HttpRequestError { get; }
        public long StatusCode { get; }

        public HttpRequestException(HttpRequestError httpRequestError, long statusCode)
        {
            HttpRequestError = httpRequestError;
            StatusCode = statusCode;
        }
        
        public HttpRequestException(HttpRequestError httpRequestError, long statusCode, string message)
            : base(message)
        {
            HttpRequestError = httpRequestError;
            StatusCode = statusCode;
        }

        public HttpRequestException(HttpRequestError httpRequestError, long statusCode, string message, Exception inner)
            : base(message, inner)
        {
            HttpRequestError = httpRequestError;
            StatusCode = statusCode;
        }
    }
    
    public class HttpResponseException : HttpException
    {
        public HttpResponseError HttpResponseError { get; }

        public HttpResponseException(HttpResponseError httpResponseError)
        {
            HttpResponseError = httpResponseError;
        }
        
        public HttpResponseException(HttpResponseError httpResponseError, string message)
            : base(message)
        {
            HttpResponseError = httpResponseError;
        }

        public HttpResponseException(HttpResponseError httpResponseError, string message, Exception inner)
            : base(message, inner)
        {
            HttpResponseError = httpResponseError;
        }
    }
}