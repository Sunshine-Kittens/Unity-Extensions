using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.WebAPI
{
    /// <summary>
    /// Applies authentication to an outgoing request.
    /// <para>
    /// This is awaited while the request is being sent rather than while it is being built, because
    /// obtaining a credential is frequently asynchronous — an expired token has to be refreshed
    /// before it can be attached. Request construction stays synchronous as a result.
    /// </para>
    /// <para>
    /// Assign an implementation to <see cref="WebServiceProvider{TServiceType}.Authenticator"/>. It is
    /// deliberately not limited to bearer tokens: a provider that only needs a fixed API-key header
    /// is an equally valid implementation.
    /// </para>
    /// </summary>
    public interface IRequestAuthenticator
    {
        ValueTask AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken = default);
    }
}
