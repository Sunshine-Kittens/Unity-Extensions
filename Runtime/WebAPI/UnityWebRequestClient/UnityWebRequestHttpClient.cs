using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine.Networking;

namespace UnityEngine.Extension.WebAPI
{
    public class UnityWebRequestHttpClient : IHttpClient
    {
        private sealed class UnitWebRequestResponse : IHttpResponse, IHttpResponseBody
        {
            // Response
            //GetResponseHeaders already returns a case-insensitive dictionary, so mixed-case lookups
            //("Content-Type" here, "etag" in SKServiceResponse) resolve correctly without re-keying.
            public IReadOnlyDictionary<string, string> Headers => _webRequest.GetResponseHeaders();
            public long StatusCode => _webRequest.responseCode;
            public IHttpResponseBody Body => this;

            // Body
            public ulong TotalBytes
            {
                get
                {
                    if (_totalBytes == 0)
                    {
                        string lengthHeader = _webRequest.GetResponseHeader("Content-Length");
                        if (!string.IsNullOrEmpty(lengthHeader))
                        {
                            ulong.TryParse(lengthHeader, out _totalBytes);
                        }   
                    }
                    return _totalBytes;
                }    
            }
            private ulong  _totalBytes; 
            
            public ulong ReceivedBytes
            {
                get
                {
                    if (_webRequest.downloadHandler != null)
                    {
                        return _webRequest.downloadedBytes;
                    }
                    return 0;
                }
            }

            public float PercentComplete
            {
                get
                {
                    if (_webRequest.downloadHandler != null)
                    {
                        if (TotalBytes > 0)
                        {
                            return ReceivedBytes / (float)TotalBytes;
                        }
                        return 0.0F;
                    }
                    return 1.0F;
                }
            }
            
            public bool IsCompleted => _asyncOperation.isDone;

            public byte[] Data
            {
                get
                {
                    if (_webRequest.downloadHandler != null)
                    {
                        return _webRequest.downloadHandler.data;
                    }
                    return Array.Empty<byte>();
                }
            }

            private bool _disposed;
            private readonly UnityWebRequest _webRequest;
            private readonly UnityWebRequestAsyncOperation _asyncOperation;
            
            public UnitWebRequestResponse(UnityWebRequest webRequest,  UnityWebRequestAsyncOperation asyncOperation)
            {
                _webRequest = webRequest;
                _asyncOperation = asyncOperation;
            }

            async ValueTask IHttpResponse.WaitForCompletionAsync(CancellationToken cancellationToken)
            {
                if (_webRequest.responseCode == 0 && _webRequest.result == UnityWebRequest.Result.InProgress)
                {
                    using (cancellationToken.Register(_webRequest.Abort))
                    {
                        while (_webRequest.responseCode == 0 && _webRequest.result == UnityWebRequest.Result.InProgress)
                        {
                            await UnityEngine.Awaitable.NextFrameAsync(cancellationToken);
                        }
                    }   
                }

                switch (_webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        throw new HttpTransportException(HttpRequestError.ConnectionError, _webRequest.responseCode, _webRequest.error);
                    case UnityWebRequest.Result.ProtocolError:
                        throw new HttpTransportException(HttpRequestError.ProtocolError, _webRequest.responseCode, _webRequest.error);
                    case UnityWebRequest.Result.DataProcessingError:
                        throw new HttpTransportException(HttpRequestError.Unknown, _webRequest.responseCode, _webRequest.error);
                }
            }
            
            async ValueTask IHttpResponseBody.WaitForCompletionAsync(CancellationToken cancellationToken)
            {
                if (!_asyncOperation.isDone)
                {
                    using (cancellationToken.Register(_webRequest.Abort))
                    {
                        while (!_asyncOperation.isDone)
                        {
                            await UnityEngine.Awaitable.NextFrameAsync(cancellationToken);
                        }
                    }   
                }

                switch (_webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        throw new HttpTransportException(HttpRequestError.ConnectionError, _webRequest.responseCode, _webRequest.error);
                    case UnityWebRequest.Result.ProtocolError:
                        throw new HttpTransportException(HttpRequestError.ProtocolError, _webRequest.responseCode, _webRequest.error);
                    case UnityWebRequest.Result.DataProcessingError:
                        throw new HttpTransportException(HttpRequestError.Unknown, _webRequest.responseCode, _webRequest.error);
                }
            }
            
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _webRequest.Dispose();
                }
            }
        }
        
        public async ValueTask<IHttpResponse> SendAsync(HttpRequest request, CancellationToken cancellationToken = default)
        {
            UnityWebRequest unityWebRequest = null;
            IHttpResponse response = null;
            try
            {
                unityWebRequest = ConvertToUnityWebRequest(request);
                UnityWebRequestAsyncOperation asyncOperation = unityWebRequest.SendWebRequest();
                response = new UnitWebRequestResponse(unityWebRequest, asyncOperation);
                await response.WaitForCompletionAsync(cancellationToken);
                return response;
            }
            catch (Exception)
            {
                response?.Dispose();   
                unityWebRequest?.Dispose();
                throw;
            }
        }

        public async ValueTask<IHttpResponse> ScheduleAsync(HttpRequest request, float seconds, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Mathf.RoundToInt(seconds * 1000.0F), cancellationToken);
            return await SendAsync(request, cancellationToken);
        }
        
        private UnityWebRequest ConvertToUnityWebRequest(HttpRequest httpRequest)
        {
            UnityWebRequest webRequest = new UnityWebRequest(httpRequest.Url, httpRequest.Method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = httpRequest.Options.RequestTimeoutInSeconds
            };

            //A redirect limit of 0 disables redirect following outright, which is never what a
            //default-constructed HttpOptions means. Treat 0 as "keep the UnityWebRequest default".
            if (httpRequest.Options.RedirectLimit > 0)
            {
                webRequest.redirectLimit = httpRequest.Options.RedirectLimit;
            }

            if (httpRequest.Body != null && httpRequest.Body.Length > 0)
            {
                webRequest.uploadHandler = new UploadHandlerRaw(httpRequest.Body);
            }

            if (httpRequest.Options.DefaultHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in httpRequest.Options.DefaultHeaders)
                {
                    webRequest.SetRequestHeader(header.Key, header.Value);
                }
            }
            
            if (httpRequest.Headers != null)
            {
                foreach (KeyValuePair<string, string> header in httpRequest.Headers)
                {
                    webRequest.SetRequestHeader(header.Key, header.Value);
                }
            }
            return webRequest;
        }
    }
}
