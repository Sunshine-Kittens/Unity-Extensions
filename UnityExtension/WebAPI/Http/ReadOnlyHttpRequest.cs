using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public struct ReadOnlyHttpRequest
    {
        private HttpRequest _request;

        public ReadOnlyHttpRequest(HttpRequest request)
        {
            _request = request;
        }

        public string Method { get { return _request.Method; } }
        public string Url { get { return _request.Url; } }
        public IReadOnlyDictionary<string, string> Headers { get { return _request.Headers; } }
        public byte[] Body { get { return _request.Body; } }
    }
}
