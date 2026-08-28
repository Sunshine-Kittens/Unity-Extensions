using System.Threading.Tasks;

namespace UnityEngine.Extension
{
    /// <summary>
    /// A pool whose template comes from an asset that has to be loaded and handed back. Owns the part that
    /// is the same however the asset is addressed: one load however many callers arrive while it is in
    /// flight, releasing a spent attempt before starting another, and discarding the result of a load that
    /// resolved after the asset was released.
    /// <para>
    /// That logic used to be copied into each concrete pool, which is where three separate defects lived.
    /// It sits here once so it can be reasoned about - and tested - in one place.
    /// </para>
    /// </summary>
    public abstract class AssetBackedObjectPool : ObjectPool
    {
        private Task<GameObject> _loadTask;

        // Bumped by every release. A load that was in flight when the asset went back resolves against
        // something already handed over, so its result has to be discarded - and the comparison is the
        // only way to tell, since the await itself completes normally.
        private int _loadGeneration;

        protected AssetBackedObjectPool() { }

        protected AssetBackedObjectPool(int capacity) : base(capacity) { }

        /// <summary>The loaded template, or null when nothing usable is loaded.</summary>
        protected abstract GameObject Template { get; }

        /// <summary>
        /// Starts a load and returns its task, or null when there is nothing to load from - an unset
        /// reference, a missing address. Only called with no load already in flight.
        /// </summary>
        protected abstract Task<GameObject> BeginLoad();

        /// <summary>
        /// Hands back whatever <see cref="BeginLoad"/> took. Must tolerate being called when nothing is
        /// held, and must leave a subsequent <see cref="BeginLoad"/> able to start cleanly.
        /// </summary>
        protected abstract void ReleaseLoaded();

        /// <summary>
        /// The loaded template, loading it first if need be. Null if the load failed, or if the asset was
        /// released while this call was waiting on it.
        /// </summary>
        protected async Awaitable<GameObject> GetTemplate()
        {
            GameObject loaded = Template;
            if (loaded != null)
            {
                return loaded;
            }

            if (_loadTask == null)
            {
                // An attempt that resolved to nothing still holds whatever it took. Release it before
                // asking for another, or that one is assigned over and never handed back.
                ReleaseAsset();

                _loadTask = BeginLoad();

                if (_loadTask == null)
                {
                    return null;
                }
            }

            int generation = _loadGeneration;

            try
            {
                GameObject result = await _loadTask;

                // Released while this was resolving: the asset behind the result has already gone back, so
                // it is not ours to hand out.
                return generation == _loadGeneration ? result : null;
            }
            finally
            {
                // Cleared by whichever caller finishes first; the rest are no-ops. A failed load leaves the
                // gate open so the next attempt can try again. Left alone if a release happened meanwhile,
                // since that already cleared the gate and opened a new generation.
                if (generation == _loadGeneration)
                {
                    _loadTask = null;
                }
            }
        }

        /// <summary>Hands the asset back and invalidates any load still in flight against it.</summary>
        protected void ReleaseAsset()
        {
            _loadGeneration++;
            _loadTask = null;
            ReleaseLoaded();
        }

        protected override void OnPoolEmpty()
        {
            ReleaseAsset();
        }
    }
}
