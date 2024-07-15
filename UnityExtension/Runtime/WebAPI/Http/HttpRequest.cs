using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public class HttpRequest
    {
        public string Method { get; private set; }
        public string Url { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public byte[] Body { get; private set; }
        public HttpOptions Options { get; private set; }

        public HttpRequest() {}

        public HttpRequest(string method, string url, Dictionary<string, string> headers, byte[] body)
        {
            Method = method;
            Url = url;
            Headers = headers;
            Body = body;
        }

        public void SetMethod(string method)
        {
            Method = method;
        }

        public void SetUrl(string url)
        {
            Url = url;
        }

        public void SetHeader(string key, string value)
        {
            if (Headers == null)
            {
                Headers = new Dictionary<string, string>(1);
            }
            Headers[key] = value;
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            Headers = headers;
        }

        public void SetBody(byte[] body)
        {
            Body = body;
        }

        public void SetOptions(HttpOptions options)
        {
            Options = options;
        }

        public void SetRedirectLimit(int redirectLimit)
        {
            HttpOptions options = Options;
            options.RedirectLimit = redirectLimit;
            Options = options;  
        }

        public void SetTimeOutInSeconds(int timeout)
        {
            HttpOptions options = Options;
            options.RequestTimeoutInSeconds = timeout;
            Options = options;
        }
    }
}
