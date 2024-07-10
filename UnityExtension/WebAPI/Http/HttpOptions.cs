namespace UnityEngine.Extension.WebAPI
{
    public struct HttpOptions
    {
        public int RequestTimeoutInSeconds;
        public int RedirectLimit;

        public HttpOptions(int requestTimeoutInSeconds, int redirectLimit)
        {
            RequestTimeoutInSeconds = requestTimeoutInSeconds;
            RedirectLimit = redirectLimit;
        }
    }
}
