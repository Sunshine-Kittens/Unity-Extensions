using System.Collections.Generic;

namespace UnityEngine.Extension.WebAPI
{
    public abstract class ServiceProvider<TServiceType> where TServiceType : ServiceProvider<TServiceType>, new()
    {
        public static TServiceType Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TServiceType();
                }
                return _instance;
            }
        }
        private static TServiceType _instance = null;

        public ServiceProvider(string defaultEnvironment, Dictionary<string, ServiceProviderEnvironment> environments)
        {
            _activeEnvironment = defaultEnvironment;
            _environments = environments;
        }

        /// <summary>
        /// Applies authentication to every request this provider sends. Null leaves requests
        /// unauthenticated. Awaited during send, so implementations may refresh credentials.
        /// </summary>
        public IRequestAuthenticator Authenticator { get; set; }

        private string _activeEnvironment;
        private Dictionary<string, ServiceProviderEnvironment> _environments;

        public string ActiveEnvironment { get { return _activeEnvironment; } }
        public ServiceProviderEnvironment Environment { get { return _environments[_activeEnvironment]; } }

        public bool SetEnvironment(string environment)
        {
            if (_environments.ContainsKey(environment))
            {
                _activeEnvironment = environment;
                return true;
            }
            //Callers routinely discard the return value, so an unknown key would otherwise leave the provider
            //silently pointing at the default environment for the lifetime of the process.
            Debug.LogError($"[{typeof(TServiceType).Name}] Unknown environment '{environment}'. " +
                $"Staying on '{_activeEnvironment}'. Known environments: {string.Join(", ", _environments.Keys)}.");
            return false;
        }

        public string GetUrl()
        {
            ServiceProviderEnvironment environment;
            if (_environments.TryGetValue(_activeEnvironment, out environment))
            {
                return environment.Url;
            }
            return string.Empty;
        }

        public HttpOptions GetDefaultOptions()
        {
            ServiceProviderEnvironment environment;
            if (_environments.TryGetValue(_activeEnvironment, out environment))
            {
                return environment.DefaultOptions;
            }
            return default;
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
        /// the <see cref="ServiceRequest{TServiceProvider, TServiceResponse}"/>. Synchronous by
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