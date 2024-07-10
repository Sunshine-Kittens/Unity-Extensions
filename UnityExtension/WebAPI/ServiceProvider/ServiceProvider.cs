using System.Collections.Generic;
using System.IO;

namespace UnityEngine.Extension.WebAPI
{
    public abstract class ServiceProvider<ServiceType> where ServiceType : ServiceProvider<ServiceType>, new()
    {
        public static ServiceProvider<ServiceType> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceType();
                }
                return _instance;
            }
        }
        private static ServiceProvider<ServiceType> _instance = null;

        private ServiceProvider() { }

        public ServiceProvider(string defaultEnvironment, Dictionary<string, ServiceProviderEnvironment> environments)
        {
            _activeEnvironment = defaultEnvironment;    
            _environments = environments;
        }

        private string _activeEnvironment;
        private Dictionary<string, ServiceProviderEnvironment> _environments;

        public bool SetEnvironment(string environment)
        {
            if(_environments.ContainsKey(environment))
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

        public virtual HttpRequest CreateRequest(string resourcePath)
        {
            string url = BuildUrl(GetUrl(), resourcePath);
            HttpRequest request = new HttpRequest();
            request.SetUrl(url);
            request.SetOptions(GetDefaultOptions());
            return request;
        }

        private static string BuildUrl(string baseUrl, string resourcePath)
        {
            //Replace '\' by '/' to unify separators used in the URL and make sure it is compatible with all platforms.
            return Path.Combine(baseUrl, resourcePath).Replace('\\', '/');
        }
    }
}