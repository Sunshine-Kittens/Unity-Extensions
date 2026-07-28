using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Extension.WebAPI
{
    public static class HttpResponseExtensions
    {
        public static bool IsSuccessStatusCode(this IHttpResponse self)
        {
            return self.StatusCode >= 200 && self.StatusCode < 300;
        }
        
        public static void EnsureSuccessStatusCode(this IHttpResponse self)
        {
            if (!IsSuccessStatusCode(self))
            {
                throw new HttpRequestException(HttpRequestError.ProtocolError, self.StatusCode);
            }
        }
        
        public static bool TryGetStringFromData(this IHttpResponse self, out string dataString)
        {
            try
            {
                dataString = GetStringFromData(self);
                return true;
            }
            catch
            {
                dataString = null;
                return false;
            }
        }

        public static string GetStringFromData(this IHttpResponse self)
        {
            //A missing Content-Type is not an error. Servers omit it on 204s and on some error responses,
            //so fall back to UTF-8 rather than refusing to read a body that is sitting right there.
            TryGetContentType(self, out string contentType);
            return GetTextEncoder(contentType).GetString(self.Body.Data);
        }

        public static bool TryGetContentType(this IHttpResponse self, out string contentType)
        {
            contentType = null;
            return self.Headers != null && self.Headers.TryGetValue("Content-Type", out contentType);
        }

        public static string GetContentType(this IHttpResponse self)
        {
            return self.Headers["Content-Type"];
        }

        public static bool TryGetJsonFromData(this IHttpResponse self, out JObject jsonObject)
        {
            try
            {
                jsonObject = GetJsonFromData(self);
                return jsonObject != null;
            }
            catch (JsonException)
            {
                jsonObject = null;
                return false;
            }
        }

        public static JObject GetJsonFromData(this IHttpResponse self)
        {
            string body = GetStringFromData(self);
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }
            return JObject.Parse(body);
        }

        public static bool TryJsonDeserializeFromData<T>(this IHttpResponse self, out T result)
        {
            try
            {
                result = JsonDeserializeFromData<T>(self);
                return true;
            }
            catch (JsonException)
            {
                result = default;
                return false;
            }
        }

        public static T JsonDeserializeFromData<T>(this IHttpResponse self)
        {
            string body = GetStringFromData(self);
            //An empty body (204 No Content, or an endpoint that returns nothing) yields default rather than throwing.
            if (string.IsNullOrWhiteSpace(body))
            {
                return default;
            }
            return JsonConvert.DeserializeObject<T>(body);
        }

        private static Encoding GetTextEncoder(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return Encoding.UTF8;
            int num = contentType.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
            if (num > -1)
            {
                int num2 = contentType.IndexOf('=', num);
                if (num2 > -1)
                {
                    string text = contentType.Substring(num2 + 1).Trim().Trim('\'', '"').Trim();
                    int num3 = text.IndexOf(';');
                    if (num3 > -1)
                    {
                        text = text.Substring(0, num3);
                    }

                    try
                    {
                        return Encoding.GetEncoding(text);
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.LogWarning($"Unsupported encoding '{text}': {ex.Message}");
                    }
                    catch (NotSupportedException ex2)
                    {
                        Debug.LogWarning($"Unsupported encoding '{text}': {ex2.Message}");
                    }
                }
            }
            return Encoding.UTF8;
        }
    }
}
