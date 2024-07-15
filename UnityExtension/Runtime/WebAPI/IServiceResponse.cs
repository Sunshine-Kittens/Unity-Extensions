namespace UnityEngine.Extension.WebAPI
{
    public interface IServiceResponse
    {
        public ReadOnlyHttpResponse HttpResponse { get; }

        public bool ProcessResponse(ReadOnlyHttpResponse httpResponse);
    }
}
