namespace UnityEngine.Extension.WebAPI
{
    /// <summary>
    /// A response whose body is a single JSON document deserialised into <typeparamref name="T"/>.
    /// <para>
    /// This exists so that the common case — an endpoint that returns one JSON object — needs no
    /// bespoke response class at all. Only responses that genuinely do something unusual with the
    /// body need to derive from <see cref="WebServiceResponse"/> directly.
    /// </para>
    /// </summary>
    public class JsonWebServiceResponse<T> : WebServiceResponse
    {
        public T Result { get; private set; }

        public JsonWebServiceResponse(IHttpResponse httpResponse) : base(httpResponse) { }

        protected override bool ProcessBodyData()
        {
            Result = JsonDeserializeFromData<T>();
            //A null result means the body was absent or literal "null", which is a failure for a
            //response that is contractually meant to carry an object. Always true for value types.
            return Result != null;
        }
    }
}
