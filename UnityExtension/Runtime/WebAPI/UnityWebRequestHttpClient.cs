using System.Threading.Tasks;

using UnityEngine.Networking;

namespace UnityEngine.Extension.WebAPI
{
    public class UnityWebRequestHttpClient : IHttpClient
    {
        public async Task<ReadOnlyHttpResponse> Send(HttpRequest request)
        {
            UnityWebRequest webRequest = request.ConvertToUnityWebRequest();
            HttpResponse response = await webRequest.SendWebRequest();
            response.SetRequest(request);
            ReadOnlyHttpResponse responseHandle = new ReadOnlyHttpResponse(response);
            webRequest.Dispose();
            return responseHandle;
        }

        public async Task<ReadOnlyHttpResponse> Schedule(HttpRequest request, float seconds)
        {
            await Task.Delay(Mathf.RoundToInt(seconds * 1000.0F));
            return await Send(request);
        }
    }
}
