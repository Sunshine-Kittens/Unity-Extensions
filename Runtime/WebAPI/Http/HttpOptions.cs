using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public struct HttpOptions
    {
        public IReadOnlyDictionary<string, string> DefaultHeaders { get; }

        /// <summary>Request timeout in seconds. 0 means no timeout.</summary>
        public int RequestTimeoutInSeconds { get; }

        /// <summary>
        /// Maximum number of redirects to follow. 0 means "use the transport default" (32 for
        /// UnityWebRequest), so that a default-constructed <see cref="HttpOptions"/> does not
        /// silently disable redirect following.
        /// </summary>
        public int RedirectLimit { get; }

        public HttpOptions(Dictionary<string, string> defaultHeaders, int requestTimeoutInSeconds, int redirectLimit)
        {
            DefaultHeaders = defaultHeaders;
            RequestTimeoutInSeconds = requestTimeoutInSeconds;
            RedirectLimit = redirectLimit;
        }
    }
}
