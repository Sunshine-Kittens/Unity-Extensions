using UnityEngine;

namespace UnityEngine.Extension.WebAPI
{
    public abstract class WebServiceResponse : IServiceResponse
    {
        public ReadOnlyHttpResponse HttpResponse { get; private set; }

        public bool ProcessResponse(in ReadOnlyHttpResponse response)
        {
            HttpResponse = response;
            return ProcessResponse();
        }
        
        protected abstract bool ProcessResponse();
    }
}
