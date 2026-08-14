using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    public abstract class WebServiceRequest<TServiceProvider, TServiceResponse> where TServiceProvider : WebServiceProvider<TServiceProvider>, new()
        where  TServiceResponse : WebServiceResponse
    {
        public ReadOnlyHttpRequest HttpRequest { get; private set; }
     
        protected abstract TServiceProvider ServiceProvider { get; }
        protected abstract HttpVerb Method { get; }
        protected abstract string ResourcePath { get; }

        /// <summary>
        /// Whether <see cref="WebServiceProvider{TServiceType}.Authenticator"/> is applied to this
        /// request. Override to false for endpoints that must be sent unauthenticated.
        /// </summary>
        protected virtual bool RequiresAuth => true;

        protected WebServiceRequest() { }

        protected abstract TServiceResponse CreateResponse(IHttpResponse httpResponse);

        /// <summary>
        /// Sends the request and reads the response body before returning.
        /// </summary>
        public ValueTask<TServiceResponse> SendAsync(CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        /// <summary>
        /// Sends the request, completing either once the body has been read
        /// (<see cref="HttpCompletionOption.ResponseContentRead"/>, the default) or as soon as the
        /// headers arrive (<see cref="HttpCompletionOption.ResponseHeadersRead"/>, leaving the caller
        /// to drive <see cref="WebServiceResponse.ProcessBodyDataAsync"/> so it can observe progress).
        /// </summary>
        public async ValueTask<TServiceResponse> SendAsync(HttpCompletionOption completionOption, CancellationToken cancellationToken = default)
        {
            //default rather than new: a request that adds no query parameters allocates nothing here.
            QueryBuilder query = default;
            BuildQuery(ref query);

            HttpRequest request = ServiceProvider.CreateRequest(Method, query.AppendTo(ResourcePath));
            PopulateRequest(request);

            if (RequiresAuth)
            {
                IRequestAuthenticator authenticator = ServiceProvider.Authenticator;
                if (authenticator != null)
                {
                    await authenticator.AuthenticateAsync(request, cancellationToken);
                }
            }

            HttpRequest = new ReadOnlyHttpRequest(request);
            IHttpResponse httpResponse = null;
            TServiceResponse serviceResponse = null;
            try
            {
                httpResponse = await HttpClient.Instance.SendAsync(request, cancellationToken);
                serviceResponse = CreateResponse(httpResponse);
                try
                {
                    if (!serviceResponse.ProcessResponse())
                    {
                        throw new HttpResponseException(HttpResponseError.ResponseProcessingError);
                    }
                }
                catch (Exception exception) when (exception is not HttpResponseException)
                {
                    throw new HttpResponseException(HttpResponseError.ResponseProcessingError, exception.Message, exception);
                }

                if (completionOption == HttpCompletionOption.ResponseContentRead)
                {
                    await serviceResponse.ProcessBodyDataAsync(cancellationToken);
                }
                return serviceResponse;
            }
            catch (Exception)
            {
                serviceResponse?.Dispose();
                httpResponse?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Adds headers and a body to the request. Optional — with authentication applied by the
        /// provider, a plain GET typically has nothing to add.
        /// </summary>
        protected virtual void PopulateRequest(HttpRequest request) { }

        /// <summary>
        /// Adds query parameters to the request. Preferred over interpolating them into
        /// <see cref="ResourcePath"/>, which bypasses percent-encoding.
        /// </summary>
        //by ref because QueryBuilder is a struct that creates its buffer lazily; a by-value copy
        //would keep the buffer to itself and the caller would see an empty query.
        protected virtual void BuildQuery(ref QueryBuilder query) { }
    }
}
