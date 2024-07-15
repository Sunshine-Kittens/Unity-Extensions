using System;

namespace UnityEngine.Extension.WebAPI
{
    public static class HttpClient
    {
        private static IHttpClient _instance;

        public static IHttpClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnityWebRequestHttpClient();
                }
                return _instance;
            }
        }

        public static void Initialize(IHttpClient httpClient)
        {
            if(_instance != null)
            {
                throw new Exception("The HttpClient has already been initialized.");
            }
            _instance = httpClient;
        }
    }
}
