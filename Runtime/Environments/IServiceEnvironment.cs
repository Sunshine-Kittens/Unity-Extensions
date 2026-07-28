namespace UnityEngine.Extension
{
    /// <summary>
    /// The one thing every per-environment configuration has in common: what the environment is
    /// called. <see cref="ServiceProvider{TSelf, TConfiguration}"/> keys its environment table on
    /// this, so the name lives in exactly one place instead of being repeated as a dictionary key.
    /// <para>
    /// Everything else is domain-specific and belongs on the implementing type — a base URL and
    /// header defaults for an HTTP service, a user pool and region for an identity service.
    /// </para>
    /// </summary>
    public interface IServiceEnvironment
    {
        string Name { get; }
    }
}
