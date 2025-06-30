using System;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public abstract class ServiceRequest<TServiceProvider, TServiceResponse> where TServiceProvider : ServiceProvider<TServiceProvider>, new()
        where  TServiceResponse : ServiceResponse
    {
        public ReadOnlyHttpRequest HttpRequest { get; private set; }
     
        protected abstract TServiceProvider ServiceProvider { get; }
        protected abstract HttpMethod Method { get; }
        protected abstract string ResourcePath { get; }

        protected ServiceRequest() { }

        protected abstract TServiceResponse CreateResponse(IHttpResponse httpResponse);
        
        public async ValueTask<TServiceResponse> Send()
        {
            HttpRequest request = ServiceProvider.CreateRequest(Method, ResourcePath);
            PopulateRequest(request);
            HttpRequest = new ReadOnlyHttpRequest(request);
            IHttpResponse httpResponse = null;
            TServiceResponse serviceResponse = null;
            try
            {
                httpResponse = await HttpClient.Instance.Send(request);
                serviceResponse = CreateResponse(httpResponse);
                serviceResponse.ProcessResponse();
                return serviceResponse;
            }
            catch (Exception)
            {
                serviceResponse?.Dispose();
                httpResponse?.Dispose();
                throw;
            }
        }
        
        protected abstract void PopulateRequest(HttpRequest request);
    }
}
