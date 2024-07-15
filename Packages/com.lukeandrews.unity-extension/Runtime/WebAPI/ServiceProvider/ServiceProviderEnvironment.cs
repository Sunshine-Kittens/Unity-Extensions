namespace UnityEngine.Extension.WebAPI
{
    public struct ServiceProviderEnvironment
    {
        public string Name { get; }
        public string Url { get; }
        public HttpOptions DefaultOptions { get; }

        public ServiceProviderEnvironment(string name, string url, HttpOptions defaultOptions)
        {
            Name = name;
            Url = url;
            DefaultOptions = defaultOptions;
        }
    }
}
