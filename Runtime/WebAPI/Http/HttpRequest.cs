using System;
using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public class HttpRequest
    {
        public string Method { get; private set; }
        public string Url { get; private set; }
        
        public IReadOnlyDictionary<string, string> Headers => _headers;
        private Dictionary<string, string> _headers;
        
        public byte[] Body { get; private set; }
        
        public HttpOptions Options { get; private set; }

        private HttpRequest() { }

        public HttpRequest(HttpMethod method, string url, in HttpOptions options)
        {
            switch (method)
            {
                case HttpMethod.GET:
                    Method = "GET";
                    break;
                case HttpMethod.POST:
                    Method = "POST";
                    break;
                case HttpMethod.PUT:
                    Method = "PUT";
                    break;
                case HttpMethod.DELETE:
                    Method = "DELETE";
                    break;
                case HttpMethod.PATCH:
                    Method = "PATCH";
                    break;
                case HttpMethod.HEAD:
                    Method = "HEAD";
                    break;
                case HttpMethod.CONNECT:
                    Method = "CONNECT";
                    break;
                case HttpMethod.OPTIONS:
                    Method = "OPTIONS";
                    break;
                case HttpMethod.TRACE:
                    Method = "TRACE";
                    break;
            }
            Url = url;
            Options = options;
        }

        public void SetHeader(string key, string value)
        {
            if (_headers == null)
            {
                _headers = new Dictionary<string, string>(1);
            }
            _headers[key] = value;
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            _headers = headers;
        }

        public void SetBody(byte[] body)
        {
            Body = body;
        }
    }
}
