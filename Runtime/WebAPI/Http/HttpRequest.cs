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

        public HttpRequest(HttpVerb method, string url, in HttpOptions options)
        {
            //The enum members are spelled exactly as the wire tokens, so no mapping table is needed.
            Method = method.ToString();
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
