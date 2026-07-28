using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    /// <summary>
    /// An environment-scoped provider of HTTP requests against a single service.
    /// Adds base URLs, default options, authentication and request construction to
    /// <see cref="ServiceProvider{TSelf, TConfiguration}"/>.
    /// </summary>
    public abstract class WebServiceProvider<TServiceType> : ServiceProvider<TServiceType, WebServiceEnvironment>
        where TServiceType : WebServiceProvider<TServiceType>, new()
    {
        public WebServiceProvider(string defaultEnvironment, params WebServiceEnvironment[] environments)
            : base(defaultEnvironment, environments) { }

        /// <summary>
        /// Applies authentication to every request this provider sends. Null leaves requests
        /// unauthenticated. Awaited during send, so implementations may refresh credentials.
        /// </summary>
        public IRequestAuthenticator Authenticator { get; set; }

        public string GetUrl()
        {
            return TryGetEnvironment(ActiveEnvironment, out WebServiceEnvironment environment)
                ? environment.Url
                : string.Empty;
        }

        public HttpOptions GetDefaultOptions()
        {
            return TryGetEnvironment(ActiveEnvironment, out WebServiceEnvironment environment)
                ? environment.DefaultOptions
                : default;
        }

        public virtual HttpRequest CreateRequest(HttpVerb method, string resourcePath)
        {
            string url = BuildUrl(GetUrl(), resourcePath);
            HttpRequest request = new HttpRequest(method, url, GetDefaultOptions());
            ApplyDefaults(request);
            return request;
        }

        /// <summary>
        /// Hook for provider-wide request defaults. Runs on every request before it is populated by
        /// the <see cref="WebServiceRequest{TServiceProvider, TServiceResponse}"/>. Synchronous by
        /// design — anything that needs to await belongs on <see cref="Authenticator"/>.
        /// </summary>
        protected virtual void ApplyDefaults(HttpRequest request) { }

        private static string BuildUrl(string baseUrl, string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return baseUrl;
            }
            if (string.IsNullOrEmpty(baseUrl))
            {
                return resourcePath;
            }
            //Join with exactly one separator. Path.Combine must not be used here: it is platform dependent
            //and it discards the base url entirely when the resource path is rooted, e.g. "/graph/invitations".
            return $"{baseUrl.TrimEnd('/', '\\')}/{resourcePath.TrimStart('/', '\\')}";
        }
    }
}
