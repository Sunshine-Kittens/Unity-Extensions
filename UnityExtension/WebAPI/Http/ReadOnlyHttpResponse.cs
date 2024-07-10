using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public struct ReadOnlyHttpResponse
    {
        HttpResponse _response;

        public ReadOnlyHttpResponse(HttpResponse response)
        {
            _response = response;
        }

        public ReadOnlyHttpRequest Request { get { return _response.Request; } }
        public IReadOnlyDictionary<string, string> Headers { get { return _response.Headers; } }
        public byte[] Data { get { return _response.Data; } }
        public long StatusCode { get { return _response.StatusCode; } }
        public string ErrorMessage { get { return _response.ErrorMessage; } }
        public bool IsHttpError { get { return _response.IsHttpError; } }
        public bool IsNetworkError { get { return _response.IsNetworkError; } }
    }
}
