using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UnityEngine.Extension.WebAPI
{
    /// <summary>
    /// Builds a percent-encoded query string.
    /// <para>
    /// Null values are skipped, so optional parameters can be added unconditionally without the
    /// caller branching on each one.
    /// </para>
    /// <para>
    /// Values are formatted with <see cref="CultureInfo.InvariantCulture"/>. This matters: on a
    /// device set to a comma-decimal locale, <c>1.5f.ToString()</c> yields "1,5", which a server
    /// will reject or silently misparse.
    /// </para>
    /// <para>
    /// A mutable struct with a lazily created buffer, so a request that adds no parameters allocates
    /// nothing. Because the buffer is created on first <see cref="Add(string, string)"/>, it must be
    /// passed by <c>ref</c> — a by-value copy would take the buffer with it and leave the caller's
    /// copy empty. Its lifetime is a single
    /// <see cref="ServiceRequest{TServiceProvider, TServiceResponse}.BuildQuery"/> call; do not store
    /// one in a field.
    /// </para>
    /// </summary>
    public struct QueryBuilder
    {
        private StringBuilder _builder;

        public readonly bool IsEmpty => _builder == null || _builder.Length == 0;

        public void Add(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || value == null)
            {
                return;
            }
            _builder ??= new StringBuilder();
            _builder.Append(_builder.Length == 0 ? '?' : '&');
            _builder.Append(Uri.EscapeDataString(name));
            _builder.Append('=');
            _builder.Append(Uri.EscapeDataString(value));
        }

        public void Add(string name, bool? value)
        {
            if (value.HasValue) Add(name, value.Value ? "true" : "false");
        }

        public void Add(string name, int? value)
        {
            if (value.HasValue) Add(name, value.Value.ToString(CultureInfo.InvariantCulture));
        }

        public void Add(string name, long? value)
        {
            if (value.HasValue) Add(name, value.Value.ToString(CultureInfo.InvariantCulture));
        }

        public void Add(string name, float? value)
        {
            if (value.HasValue) Add(name, value.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        public void Add(string name, double? value)
        {
            if (value.HasValue) Add(name, value.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        public void Add(string name, Guid? value)
        {
            if (value.HasValue) Add(name, value.Value.ToString("D", CultureInfo.InvariantCulture));
        }

        /// <summary>Adds an ISO-8601 round-trip timestamp, normalised to UTC.</summary>
        public void Add(string name, DateTime? value)
        {
            if (value.HasValue) Add(name, value.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
        }

        /// <summary>Repeats the key once per value — the OpenAPI "form / explode: true" style.</summary>
        public void AddRange(string name, IEnumerable<string> values)
        {
            if (values == null)
            {
                return;
            }
            foreach (string value in values)
            {
                Add(name, value);
            }
        }

        /// <summary>Returns "" when nothing was added, otherwise "?a=b&amp;c=d".</summary>
        public readonly override string ToString()
        {
            return _builder == null ? string.Empty : _builder.ToString();
        }

        /// <summary>
        /// Appends this query to a resource path. If the path already carries a query string, the
        /// parameters are spliced on with '&amp;' rather than producing a second '?'.
        /// </summary>
        public readonly string AppendTo(string resourcePath)
        {
            if (IsEmpty)
            {
                return resourcePath;
            }
            string query = _builder.ToString();
            if (!string.IsNullOrEmpty(resourcePath) && resourcePath.IndexOf('?') >= 0)
            {
                return string.Concat(resourcePath, "&", query.Substring(1));
            }
            return string.Concat(resourcePath, query);
        }
    }
}
