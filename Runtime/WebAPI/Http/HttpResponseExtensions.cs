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
                string contentType;
                if (TryGetContentType(self, out contentType))
                {
                    Encoding encoding = GetTextEncoder(contentType);
                    dataString = encoding.GetString(self.Body.Data);
                    return true;
                }
                dataString = null;
                return false;
            }
            catch
            {
                dataString = null;
                return false;
            }
        }

        public static string GetStringFromData(this IHttpResponse self)
        {
            Encoding encoding = GetTextEncoder(GetContentType(self));
            return encoding.GetString(self.Body.Data);
        }

        public static bool TryGetContentType(this IHttpResponse self, out string contentType)
        {
            return self.Headers.TryGetValue("Content-Type", out contentType);
        }

        public static string GetContentType(this IHttpResponse self)
        {
            return self.Headers["Content-Type"];
        }

        public static bool TryGetJsonFromData(this IHttpResponse self, out JObject jsonObject)
        {
            if (TryGetContentType(self, out string contentType))
            {
                Encoding encoding = GetTextEncoder(contentType);
                try
                {
                    jsonObject = JObject.Parse(encoding.GetString(self.Body.Data));
                    return true;
                }
                catch(JsonException)
                {
                    jsonObject = null;
                    return false;
                }
            }
            jsonObject = null;
            return false;
        }
        
        public static JObject GetJsonFromData(this IHttpResponse self)
        {
            if (!TryGetContentType(self, out string contentType)) throw new InvalidOperationException("Unable to get json without valid content type.");
            Encoding encoding = GetTextEncoder(contentType);
            return JObject.Parse(encoding.GetString(self.Body.Data));
        }

        public static bool TryJsonDeserializeFromData<T>(this IHttpResponse self, out T result)
        {
            if (TryGetContentType(self, out string contentType))
            {
                Encoding encoding = GetTextEncoder(contentType);
                try
                {
                    result = JsonConvert.DeserializeObject<T>(encoding.GetString(self.Body.Data));
                    return true;
                }
                catch(JsonException)
                {
                    result = default;
                    return false;
                }
            }
            result = default;
            return false;
        }
        
        public static T JsonDeserializeFromData<T>(this IHttpResponse self)
        {
            if (!TryGetContentType(self, out string contentType)) throw new InvalidOperationException("Unable to json deserialize without valid content type.");
            Encoding encoding = GetTextEncoder(contentType);
            return JsonConvert.DeserializeObject<T>(encoding.GetString(self.Body.Data));
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
