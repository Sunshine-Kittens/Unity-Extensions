using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    /// <summary>
    /// A singleton service whose configuration is chosen by environment name.
    /// <para>
    /// Factored out of <c>WebAPI.WebServiceProvider</c> so that services which are environment-scoped
    /// but not HTTP-based — an identity provider, for instance — get the same environment handling
    /// without inheriting base URLs, header defaults and request construction they have no use for.
    /// </para>
    /// </summary>
    /// <typeparam name="TSelf">
    /// The concrete service type, so <see cref="Instance"/> is strongly typed. The type must expose a
    /// public parameterless constructor that supplies its own environment table to the base.
    /// </typeparam>
    /// <typeparam name="TConfiguration">Whatever the service needs to differ by environment.</typeparam>
    public abstract class ServiceProvider<TSelf, TConfiguration>
        where TSelf : ServiceProvider<TSelf, TConfiguration>, new()
        where TConfiguration : IServiceEnvironment
    {
        public static TSelf Instance => _instance ??= new TSelf();
        private static TSelf _instance;

        private readonly Dictionary<string, TConfiguration> _environments;

        public string ActiveEnvironment { get; private set; }

        /// <summary>Configuration for the active environment.</summary>
        public TConfiguration Environment => _environments[ActiveEnvironment];

        /// <summary>The environment names this service knows about.</summary>
        public IReadOnlyCollection<string> EnvironmentNames => _environments.Keys;

        /// <summary>
        /// Each environment supplies its own name, so the table is keyed from
        /// <see cref="IServiceEnvironment.Name"/> rather than the caller repeating it. Misconfiguration
        /// throws here, at construction, rather than surfacing as a lookup failure much later.
        /// </summary>
        protected ServiceProvider(string defaultEnvironment, params TConfiguration[] environments)
        {
            if (environments == null || environments.Length == 0)
            {
                throw new ArgumentException($"[{typeof(TSelf).Name}] At least one environment is required.", nameof(environments));
            }

            _environments = new Dictionary<string, TConfiguration>(environments.Length);
            foreach (TConfiguration environment in environments)
            {
                string name = environment.Name;
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"[{typeof(TSelf).Name}] An environment was supplied without a name.", nameof(environments));
                }
                
                if (!_environments.TryAdd(name, environment))
                {
                    throw new ArgumentException($"[{typeof(TSelf).Name}] Duplicate environment '{name}'.", nameof(environments));
                }
            }

            if (!_environments.ContainsKey(defaultEnvironment))
            {
                throw new ArgumentException($"[{typeof(TSelf).Name}] Default environment '{defaultEnvironment}' is not one of: " +
                    $"{string.Join(", ", _environments.Keys)}.", nameof(defaultEnvironment));
            }
            ActiveEnvironment = defaultEnvironment;
        }

        public bool TryGetEnvironment(string environment, out TConfiguration configuration)
        {
            return _environments.TryGetValue(environment, out configuration);
        }

        /// <summary>
        /// Switches the active environment. Returns false if the name is unknown or a subclass vetoed
        /// the change.
        /// </summary>
        public bool SetEnvironment(string environment)
        {
            if (!_environments.TryGetValue(environment, out TConfiguration configuration))
            {
                //Callers routinely discard the return value, so an unknown key would otherwise leave the
                //service silently pointing at its default environment for the lifetime of the process.
                Debug.LogError($"[{typeof(TSelf).Name}] Unknown environment '{environment}'. Staying on " +
                    $"'{ActiveEnvironment}'. Known environments: {string.Join(", ", _environments.Keys)}.");
                return false;
            }

            if (environment == ActiveEnvironment)
            {
                return true;
            }

            if (!CanSetEnvironment(environment, configuration))
            {
                return false;
            }

            ActiveEnvironment = environment;
            OnEnvironmentChanged();
            return true;
        }

        /// <summary>
        /// Veto hook, called before the change is applied. Return false to refuse it — and log why,
        /// since the base class cannot know the reason.
        /// </summary>
        protected virtual bool CanSetEnvironment(string environment, in TConfiguration configuration) => true;

        /// <summary>Called after the active environment changes. Use it to drop cached state.</summary>
        protected virtual void OnEnvironmentChanged() { }
    }
}
