namespace UnityEngine.Extension.WebAPI
{
    /// <summary>
    /// What a <see cref="WebServiceProvider{TServiceType}"/> needs to differ per environment: where
    /// the service lives and how requests to it are configured by default.
    /// </summary>
    public readonly struct WebServiceEnvironment : IServiceEnvironment
    {
        public string Name { get; }
        public string Url { get; }
        public HttpOptions DefaultOptions { get; }

        public WebServiceEnvironment(string name, string url, HttpOptions defaultOptions = default)
        {
            Name = name;
            Url = url;
            DefaultOptions = defaultOptions;
        }
    }
}
