using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public struct HttpOptions
    {
        public IReadOnlyDictionary<string, string> DefaultHeaders { get; }
        public int RequestTimeoutInSeconds { get; }
        public int RedirectLimit { get; }

        public HttpOptions(Dictionary<string, string> defaultHeaders, int requestTimeoutInSeconds, int redirectLimit)
        {
            DefaultHeaders = defaultHeaders;
            RequestTimeoutInSeconds = requestTimeoutInSeconds;
            RedirectLimit = redirectLimit;
        }
    }
}
