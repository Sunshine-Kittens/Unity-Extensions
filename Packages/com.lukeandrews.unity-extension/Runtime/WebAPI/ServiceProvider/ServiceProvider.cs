using System.Collections.Generic;
using System.IO;

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

        private ServiceProvider() { }

        public ServiceProvider(string defaultEnvironment, Dictionary<string, ServiceProviderEnvironment> environments)
        {
            _activeEnvironment = defaultEnvironment;
            _environments = environments;
        }

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
        
        public virtual HttpRequest CreateRequest(HttpMethod method, string resourcePath)
        {
            string url = BuildUrl(GetUrl(), resourcePath);
            HttpRequest request = new HttpRequest(method, url, GetDefaultOptions());
            return request;
        }

        private static string BuildUrl(string baseUrl, string resourcePath)
        {
            //Replace '\' by '/' to unify separators used in the URL and make sure it is compatible with all platforms.
            return Path.Combine(baseUrl, resourcePath).Replace('\\', '/');
        }
    }
}