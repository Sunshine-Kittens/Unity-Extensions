namespace UnityEngine.Extension.WebAPI
{
    /// <summary>
    /// Controls when a send operation completes. Mirrors <c>System.Net.Http.HttpCompletionOption</c>.
    /// </summary>
    public enum HttpCompletionOption
    {
        /// <summary>
        /// Complete once the response body has been received and processed. The returned response is
        /// fully populated and safe to read immediately. This is the default.
        /// </summary>
        ResponseContentRead = 0,

        /// <summary>
        /// Complete as soon as the response headers are available. The body is still in flight, so the
        /// caller must drive <see cref="WebServiceResponse.ProcessBodyDataAsync"/> itself. Use this when
        /// you need to observe <see cref="WebServiceResponse.PercentComplete"/> while the body downloads.
        /// </summary>
        ResponseHeadersRead = 1
    }
}
