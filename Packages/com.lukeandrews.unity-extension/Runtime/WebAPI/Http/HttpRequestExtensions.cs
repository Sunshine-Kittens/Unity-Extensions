using System;
using System.Text;
using Newtonsoft.Json;
namespace UnityEngine.Extension.WebAPI
{
    public static class HttpRequestExtensions
    {
        public static void SetContentType(this HttpRequest self, string mediaType, Encoding charset = null, params (string name, string value)[] parameters)
        {
            // Base media type + optional charset
            var sb = new StringBuilder(mediaType);
            if (charset != null)
            {
                sb.Append($"; charset={charset.WebName}");   
            }

            foreach (var (name, value) in parameters)
            {
                sb.Append($"; {name}={value}");
            }
            self.SetHeader("Content-Type", sb.ToString());
        }
        
        public static void SetContentTypeJson(this HttpRequest self, Encoding charset = null) => self.SetContentType("application/json", charset);
        public static void SetContentTypeXml(this HttpRequest self, Encoding charset = null) => self.SetContentType("application/xml", charset);
        public static void SetContentTypeHtml(this HttpRequest self, Encoding charset = null) => self.SetContentType("text/html", charset);
        public static void SetContentTypeText(this HttpRequest self, Encoding charset = null) => self.SetContentType("text/plain", charset);
        public static void SetContentTypeFormUrlEncoded(this HttpRequest self, Encoding charset = null) => self.SetContentType("application/x-www-form-urlencoded", charset);
        public static void SetContentTypeMultipartFormData(this HttpRequest self, string boundary) => self.SetContentType("multipart/form-data", null, ("boundary", boundary));
        public static void SetContentTypeJsonLd(this HttpRequest self, Encoding charset = null) => self.SetContentType("application/ld+json", charset);
        public static void SetContentTypeJsonWithProfile(this HttpRequest self, string profileUrl) => self.SetContentType("application/json", Encoding.UTF8, ("profile", $"\"{profileUrl}\""));
        public static void SetAccept(this HttpRequest self, string mimeTypes) => self.SetHeader("Accept", mimeTypes);
        public static void SetAcceptJson(this HttpRequest self) => self.SetAccept("application/json");
        public static void SetAcceptXml(this HttpRequest self) => self.SetAccept("application/xml");
        public static void SetAuthorization(this HttpRequest self, string scheme, string parameter) => self.SetHeader("Authorization", $"{scheme} {parameter}");
        public static void SetBearerToken(this HttpRequest self, string token) => self.SetAuthorization("Bearer", token);
        public static void SetUserAgent(this HttpRequest self, string userAgent) => self.SetHeader("User-Agent", userAgent);
        public static void SetCacheControl(this HttpRequest self, string directives) => self.SetHeader("Cache-Control", directives);
        public static void SetNoCache(this HttpRequest self) => self.SetCacheControl("no-cache");
        public static void SetMaxAge(this HttpRequest self, int seconds) => self.SetCacheControl($"max-age={seconds}");
        public static void SetIfModifiedSince(this HttpRequest self, DateTime dateTime) => self.SetHeader("If-Modified-Since", dateTime.ToUniversalTime().ToString("r"));
        public static void SetIfNoneMatch(this HttpRequest self, string etag) => self.SetHeader("If-None-Match", etag);
        public static void SetAcceptEncoding(this HttpRequest self, string encodings) => self.SetHeader("Accept-Encoding", encodings);
        public static void SetAcceptEncodingGzip(this HttpRequest self) => self.SetAcceptEncoding("gzip");
        public static void SetAcceptEncodingDeflate(this HttpRequest self) => self.SetAcceptEncoding("deflate");
        public static void SetAcceptLanguage(this HttpRequest self, string languages) => self.SetHeader("Accept-Language", languages);
        public static void SetHost(this HttpRequest self, string host) => self.SetHeader("Host", host);
        public static void SetReferer(this HttpRequest self, string url) => self.SetHeader("Referer", url);
        public static void SetOrigin(this HttpRequest self, string origin) => self.SetHeader("Origin", origin);
        
        public static void SetBodyJson(this HttpRequest self, object data)
        {
            string jsonString = JsonConvert.SerializeObject(data);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
            self.SetBody(bytes);
        }
    }
}
