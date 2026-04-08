using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace UnityEngine.Extension.WebAPI.Http
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
        
        public static void SetContentTypeJson(this HttpRequest self, Encoding charset = null) 
            => self.SetContentType("application/json", charset);
        
        public static void SetContentTypeXml(this HttpRequest self, Encoding charset = null) 
            => self.SetContentType("application/xml", charset);
        
        public static void SetContentTypeHtml(this HttpRequest self, Encoding charset = null) 
            => self.SetContentType("text/html", charset);
        
        public static void SetContentTypeText(this HttpRequest self, Encoding charset = null) 
            => self.SetContentType("text/plain", charset);
        
        public static void SetContentTypeFormUrlEncoded(this HttpRequest self, Encoding charset = null) 
            => self.SetContentType("application/x-www-form-urlencoded", charset);
        
        public static void SetContentTypeMultipartFormData(this HttpRequest self, string boundary) 
            => self.SetContentType("multipart/form-data", null, ("boundary", boundary));
        
        public static void SetContentTypeJsonLd(this HttpRequest self, Encoding charset = null) 
            => self.SetContentType("application/ld+json", charset);
        
        public static void SetContentTypeJsonWithProfile(this HttpRequest self, string profileUrl) 
            => self.SetContentType("application/json", Encoding.UTF8, ("profile", $"\"{profileUrl}\""));
        
        public static void SetAccept(this HttpRequest self, string mimeTypes) 
            => self.SetHeader("Accept", mimeTypes);
        
        public static void SetAcceptJson(this HttpRequest self) 
            => self.SetAccept("application/json");
        
        public static void SetAcceptXml(this HttpRequest self) 
            => self.SetAccept("application/xml");
        
        public static void SetAuthorization(this HttpRequest self, string scheme, string parameter) 
            => self.SetHeader("Authorization", $"{scheme} {parameter}");
        
        public static void SetBearerToken(this HttpRequest self, string token) 
            => self.SetAuthorization("Bearer", token);
        
        public static void SetUserAgent(this HttpRequest self, string userAgent) 
            => self.SetHeader("User-Agent", userAgent);
        
        public static void SetCacheControl(this HttpRequest self, string directives) 
            => self.SetHeader("Cache-Control", directives);
        
        public static void SetNoCache(this HttpRequest self) 
            => self.SetCacheControl("no-cache");
        
        public static void SetMaxAge(this HttpRequest self, int seconds) 
            => self.SetCacheControl($"max-age={seconds}");
        
        public static void SetIfModifiedSince(this HttpRequest self, DateTime dateTime) 
            => self.SetHeader("If-Modified-Since", dateTime.ToUniversalTime().ToString("r"));
        
        public static void SetIfNoneMatch(this HttpRequest self, string etag) 
            => self.SetHeader("If-None-Match", etag);
        
        public static void SetIfMatch(this HttpRequest self, string etag) 
            => self.SetHeader("If-Match", etag);
        
        public static void SetAcceptEncoding(this HttpRequest self, string encodings) 
            => self.SetHeader("Accept-Encoding", encodings);
        
        public static void SetAcceptEncodingGzip(this HttpRequest self) 
            => self.SetAcceptEncoding("gzip");
        
        public static void SetAcceptEncodingDeflate(this HttpRequest self) 
            => self.SetAcceptEncoding("deflate");
        
        public static void SetAcceptLanguage(this HttpRequest self, string languages) 
            => self.SetHeader("Accept-Language", languages);
        
        public static void SetHost(this HttpRequest self, string host) 
            => self.SetHeader("Host", host);
        
        public static void SetReferer(this HttpRequest self, string url) 
            => self.SetHeader("Referer", url);
        
        public static void SetOrigin(this HttpRequest self, string origin) 
            => self.SetHeader("Origin", origin);
        
        public static void SetBodyJson(this HttpRequest self, object data)
        {
            string jsonString = JsonConvert.SerializeObject(data);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
            self.SetBody(bytes);
        }

        public static void SetBodyMultipartFormData(this HttpRequest self, List<MultipartFormSection> formSections, string boundary)
        {
            byte[] boundaryBytes = Encoding.ASCII.GetBytes(boundary);
            byte[] bytes = SerializeFormSections(formSections, boundaryBytes);
            self.SetBody(bytes);
        }
        
        public static string GenerateBoundaryString(this HttpRequest self, int length = 40)
        {
            if (length <= 0 || length > 70)
                throw new ArgumentOutOfRangeException(nameof(length), "Boundary length must be between 1 and 70.");

            const string supportedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_";
            StringBuilder sb = new (length);

            for (int i = 0; i < length; i++)
            {
                int index = Random.Range(0, supportedChars.Length);
                sb.Append(supportedChars[index]);
            }
            return sb.ToString();
        }
        
        private static byte[] SerializeFormSections(this List<MultipartFormSection> formSections, byte[] boundary)
        {
            if (formSections == null || formSections.Count == 0)
                return Array.Empty<byte>();
                
            byte[] crlf = Encoding.UTF8.GetBytes("\r\n");
            byte[] dashDash = Encoding.ASCII.GetBytes("--");

            using (MemoryStream stream = new ())
            {
                bool first = true;
                foreach (MultipartFormSection section in formSections)
                {
                    if (section.SectionData == null)
                        continue;

                    if (!first)
                        stream.Write(crlf, 0, crlf.Length);

                    stream.Write(dashDash, 0, dashDash.Length);
                    stream.Write(boundary, 0, boundary.Length);
                    stream.Write(crlf, 0, crlf.Length);

                    // Build headers
                    string disposition = "Content-Disposition: form-data";
                    if (!string.IsNullOrEmpty(section.SectionName))
                        disposition += $"; name=\"{section.SectionName}\"";
                    if (!string.IsNullOrEmpty(section.FileName))
                        disposition += $"; filename=\"{section.FileName}\"";
                    disposition += "\r\n";

                    stream.Write(Encoding.UTF8.GetBytes(disposition), 0, Encoding.UTF8.GetByteCount(disposition));

                    if (!string.IsNullOrEmpty(section.ContentType))
                    {
                        string contentType = $"Content-Type: {section.ContentType}\r\n";
                        stream.Write(Encoding.UTF8.GetBytes(contentType), 0, Encoding.UTF8.GetByteCount(contentType));
                    }

                    stream.Write(crlf, 0, crlf.Length);
                    stream.Write(section.SectionData, 0, section.SectionData.Length);

                    first = false;
                }

                // Final closing boundary
                stream.Write(crlf, 0, crlf.Length);
                stream.Write(dashDash, 0, dashDash.Length);
                stream.Write(boundary, 0, boundary.Length);
                stream.Write(dashDash, 0, dashDash.Length);
                stream.Write(crlf, 0, crlf.Length);

                return stream.ToArray();
            }
        }
    }
}
