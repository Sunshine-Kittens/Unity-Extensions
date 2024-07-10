using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public class HttpResponse
    {
        public ReadOnlyHttpRequest Request { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public byte[] Data { get; private set; }
        public long StatusCode { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool IsHttpError { get; private set; }
        public bool IsNetworkError { get; private set; }

        public void SetRequest(HttpRequest request)
        {
            Request = new ReadOnlyHttpRequest(request);
        }

        public void SetRequest(ReadOnlyHttpRequest request)
        {
            Request = request;
        }

        public void SetHeader(string key, string value)
        {
            Headers[key] = value;
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            Headers = headers;
        }

        public void SetData(byte[] data)
        {
            Data = data;
        }

        public void SetStatusCode(long statusCode)
        {
            StatusCode = statusCode;
        }

        public void SetErrorMessage(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public void SetIsHttpError(bool isHttpError)
        {
            IsHttpError = isHttpError;
        }

        public void SetIsNetworkError(bool isNetworkError)
        {
            IsNetworkError = isNetworkError;
        }
    }
}
