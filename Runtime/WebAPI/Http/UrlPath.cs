using System;

namespace UnityEngine.Extension.WebAPI
{
    /// <summary>
    /// Helpers for composing resource paths.
    /// </summary>
    public static class UrlPath
    {
        /// <summary>
        /// Percent-encodes a single path segment. Use this for every interpolated path parameter —
        /// an unescaped id containing '/', '?' or '#' would otherwise silently change which resource
        /// the request addresses.
        /// </summary>
        public static string Escape(string segment)
        {
            return string.IsNullOrEmpty(segment) ? string.Empty : Uri.EscapeDataString(segment);
        }
    }
}
