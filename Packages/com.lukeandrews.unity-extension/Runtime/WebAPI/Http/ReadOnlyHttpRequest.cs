using System;
using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public readonly struct ReadOnlyHttpRequest
    {
        private readonly HttpRequest _request;

        public ReadOnlyHttpRequest(HttpRequest request)
        {
            _request = request;
        }

        public string Method
        {
            get
            {
                if (_request != null)
                {
                    return _request.Method;
                }
                return string.Empty;
            }
        }

        public string Url 
        {
            get
            {
                if (_request != null)
                {
                    return _request.Url;
                }
                return string.Empty;
            }
        }
        
        public IReadOnlyDictionary<string, string> Headers 
        {
            get
            {
                if (_request != null)
                {
                    return _request.Headers;
                }
                return null;
            }
        }
        
        public byte[] Body 
        {
            get
            {
                if (_request != null)
                {
                    return _request.Body;
                }
                return null;
            }
        }
        
        public HttpOptions Options 
        {
            get
            {
                if (_request != null)
                {
                    return _request.Options;
                }
                return default;
            }
        }
    }
}