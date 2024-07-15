using System;
using System.Text;

namespace UnityEngine.Extension.WebAPI
{
    public static class HttpResponseExtensions
    {
        public static bool TryGetStringFromData(this HttpResponse self, out string dataString)
        {
            try
            {
                string contentType;
                if(TryGetContentType(self, out contentType))
                {
                    Encoding encoding = GetTextEncoder(contentType);
                    dataString = encoding.GetString(self.Data);
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

        public static string GetStringFromData(this HttpResponse self)
        {
            Encoding encoding = GetTextEncoder(GetContentType(self));
            return encoding.GetString(self.Data);
        }

        public static bool TryGetContentType(this HttpResponse self, out string contentType)
        {
            return self.Headers.TryGetValue("Content-Type", out contentType);
        }

        public static string GetContentType(this HttpResponse self)
        {
            return self.Headers["Content-Type"];
        }

        public static bool TryGetStringFromData(this ReadOnlyHttpResponse self, out string dataString)
        {
            try
            {
                string contentType;
                if (TryGetContentType(self, out contentType))
                {
                    Encoding encoding = GetTextEncoder(contentType);
                    dataString = encoding.GetString(self.Data);
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

        public static string GetStringFromData(this ReadOnlyHttpResponse self)
        {
            Encoding encoding = GetTextEncoder(GetContentType(self));
            return encoding.GetString(self.Data);
        }

        public static bool TryGetContentType(this ReadOnlyHttpResponse self, out string contentType)
        {
            return self.Headers.TryGetValue("Content-Type", out contentType);
        }

        public static string GetContentType(this ReadOnlyHttpResponse self)
        {
            return self.Headers["Content-Type"];
        }

        private static Encoding GetTextEncoder(string contentType)
        {
            if (!string.IsNullOrEmpty(contentType))
            {
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
            }
            return Encoding.UTF8;
        }
    }
}
