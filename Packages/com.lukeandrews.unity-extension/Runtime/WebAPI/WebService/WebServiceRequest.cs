using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public abstract class WebServiceRequest<TWebServiceProvider, TWebServiceResponse> : IServiceRequest<TWebServiceResponse> 
        where TWebServiceProvider : ServiceProvider<TWebServiceProvider>, new()
        where  TWebServiceResponse : WebServiceResponse, new()
    {
        public ReadOnlyHttpRequest HttpRequest { get; private set; }
     
        protected abstract TWebServiceProvider ServiceProvider { get; }
        protected abstract string ResourcePath { get; }

        public abstract void Dispose();

        public async Task<TWebServiceResponse> Send()
        {
            HttpRequest request = ServiceProvider.CreateRequest(ResourcePath);
            PopulateRequest(request);
            HttpRequest = new ReadOnlyHttpRequest(request);
            ReadOnlyHttpResponse readOnlyResponse = await HttpClient.Instance.Send(request);
            TWebServiceResponse response = new TWebServiceResponse();
            response.ProcessResponse(readOnlyResponse);
            return response;
        }
        
        protected abstract void PopulateRequest(HttpRequest request);
    }
}
