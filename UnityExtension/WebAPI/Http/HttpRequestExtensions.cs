namespace UnityEngine.Extension.WebAPI
{
    public static class HttpRequestExtensions
    {
        public static void SetMethodGet(this HttpRequest self)
        {
            self.SetMethod("GET");
        }

        public static void SetMethodPost(this HttpRequest self)
        {
            self.SetMethod("POST");
        }

        public static void SetMethodPut(this HttpRequest self)
        {
            self.SetMethod("PUT");
        }

        public static void SetMethodDelete(this HttpRequest self)
        {
            self.SetMethod("DELETE");
        }

        public static void SetMethodPatch(this HttpRequest self)
        {
            self.SetMethod("PATCH");
        }

        public static void SetMethodHead(this HttpRequest self)
        {
            self.SetMethod("HEAD");
        }

        public static void SetMethodConnect(this HttpRequest self)
        {
            self.SetMethod("CONNECT");
        }

        public static void SetMethodOptions(this HttpRequest self)
        {
            self.SetMethod("OPTIONS");
        }

        public static void SetMethodTrace(this HttpRequest self)
        {
            self.SetMethod("TRACE");
        }

        public static void SetContentType(this HttpRequest self, string contentType)
        {
            self.SetHeader("Content-Type", contentType);
        }

        public static void SetContentTypeJson(this HttpRequest self)
        {
            self.SetHeader("Content-Type", "application/json");
        }
    }
}
