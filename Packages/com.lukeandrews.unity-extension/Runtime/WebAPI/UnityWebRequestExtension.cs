using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using UnityEngine.Networking;

namespace UnityEngine.Extension.WebAPI
{
    internal static class UnityWebRequestExtensions
    {
        public static UnityWebRequest ConvertToUnityWebRequest(this HttpRequest self)
        {
            UnityWebRequest webRequest = new UnityWebRequest(self.Url, self.Method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                redirectLimit = self.Options.RedirectLimit,
                timeout = self.Options.RequestTimeoutInSeconds
            };

            if (self.Body != null && self.Body.Length > 0)
            {
                webRequest.uploadHandler = new UploadHandlerRaw(self.Body);
            }

            if (self.Headers != null)
            {
                foreach (KeyValuePair<string, string> header in self.Headers)
                {
                    webRequest.SetRequestHeader(header.Key, header.Value);
                }
            }
            return webRequest;
        }

        public static HttpResponse ConvertToResponse(this UnityWebRequest self)
        {
            HttpResponse response = new HttpResponse();
            response.SetHeaders(self.GetResponseHeaders());
            response.SetData(self.downloadHandler?.data);
            response.SetStatusCode(self.responseCode);
            response.SetErrorMessage(self.error);
            response.SetIsHttpError(self.result == UnityWebRequest.Result.ProtocolError);
            response.SetIsNetworkError(self.result == UnityWebRequest.Result.ConnectionError);
            return response;
        }

        public static TaskAwaiter<HttpResponse> GetAwaiter(this UnityWebRequestAsyncOperation webRequestAsyncOp)
        {
            TaskCompletionSource<HttpResponse> completionSource = new TaskCompletionSource<HttpResponse>();
            if (webRequestAsyncOp.isDone)
            {
                OnCompleted(completionSource, webRequestAsyncOp);
            }
            else
            {
                webRequestAsyncOp.completed += delegate (AsyncOperation asyncOperation)
                {
                    OnCompleted(completionSource, (UnityWebRequestAsyncOperation)asyncOperation);
                };
            }
            return completionSource.Task.GetAwaiter();
        }

        private static void OnCompleted(TaskCompletionSource<HttpResponse> completionSource, UnityWebRequestAsyncOperation asyncOperation)
        {
            HttpResponse response = asyncOperation.webRequest.ConvertToResponse();
            if (response.IsHttpError || response.IsNetworkError)
            {
                completionSource.SetException(new HttpRequestException(response));
            }
            else
            {
                completionSource.SetResult(response);
            }
        }
    }
}
