using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace UnityEngine.Extension.WebAPI
{
    public abstract class ServiceResponse : IDisposable
    {
        // Response
        public IReadOnlyDictionary<string, string> Headers => _httpResponse.Headers;
        public long StatusCode => _httpResponse.StatusCode;

        // Body
        public ulong TotalBytes => _httpResponseBody.TotalBytes;
        public ulong ReceivedBytes => _httpResponseBody.ReceivedBytes;
        public float PercentComplete => _httpResponseBody.PercentComplete;
        public bool IsCompleted => _httpResponseBody.IsCompleted;
        public byte[] Data => _httpResponseBody.Data;

        private IHttpResponseBody _httpResponseBody => _httpResponse.Body ?? IHttpResponseBody.Empty;
        
        private readonly IHttpResponse _httpResponse;
        private bool _disposed;

        protected ServiceResponse(IHttpResponse httpResponse)
        {
            _httpResponse = httpResponse ?? throw new ArgumentNullException(nameof(httpResponse));
        }

        /// <summary>True once the response body has been received and processed.</summary>
        public bool IsBodyProcessed { get; private set; }

        /// <summary>
        /// Inspects the response headers and status. Runs as soon as the headers arrive, before the
        /// body. Return false to reject the response.
        /// </summary>
        public virtual bool ProcessResponse() => true;

        /// <summary>
        /// Waits for the body to arrive and processes it. Idempotent — sending with
        /// <see cref="HttpCompletionOption.ResponseContentRead"/> already drove this, so calling it
        /// again is a no-op rather than a second parse.
        /// </summary>
        public async ValueTask ProcessBodyDataAsync(CancellationToken cancellationToken = default)
        {
            if (IsBodyProcessed)
            {
                return;
            }

            await _httpResponse.Body.WaitForCompletionAsync(cancellationToken);
            try
            {
                if (!ProcessBodyData())
                {
                    throw new HttpResponseException(HttpResponseError.DataProcessingError);
                }
            }
            catch (Exception exception) when (exception is not HttpResponseException)
            {
                throw new HttpResponseException(HttpResponseError.DataProcessingError, exception.Message, exception);
            }
            IsBodyProcessed = true;
        }

        /// <summary>
        /// Deserialises the body. Defaults to a no-op so that responses carrying no content need not
        /// override it. Return false to reject the body.
        /// </summary>
        protected virtual bool ProcessBodyData() => true;

        public void Dispose()
        {
            if (_disposed) return;
            _httpResponse.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        
        public bool IsSuccessStatusCode()
        {
            return _httpResponse.IsSuccessStatusCode();
        }
        
        public void EnsureSuccessStatusCode()
        {
            _httpResponse.EnsureSuccessStatusCode();
        }
        
        public bool TryGetStringFromData(out string dataString)
        {
            return _httpResponse.TryGetStringFromData(out dataString);
        }

        public string GetStringFromData()
        {
            return _httpResponse.GetStringFromData();
        }

        public bool TryGetContentType(out string contentType)
        {
            return _httpResponse.TryGetContentType(out contentType);
        }

        public string GetContentType()
        {
            return _httpResponse.GetContentType();
        }

        public bool TryGetJsonFromData(out JObject jsonObject)
        {
            return _httpResponse.TryGetJsonFromData(out jsonObject);
        }
        
        public JObject GetJsonFromData()
        {
            return _httpResponse.GetJsonFromData();
        }

        public bool TryJsonDeserializeFromData<T>(out T result)
        {
            return _httpResponse.TryJsonDeserializeFromData<T>(out result);
        }
        
        public T JsonDeserializeFromData<T>()
        {
            return _httpResponse.JsonDeserializeFromData<T>();
        }
    }
}