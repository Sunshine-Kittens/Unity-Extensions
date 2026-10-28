namespace UnityEngine.Extension
{
    /// <summary>
    /// Opts a component into its object's pooling lifecycle. Put one (or several) on a pooled prefab and
    /// the pool calls them at each transition - no bookkeeping to implement, no events to unsubscribe, and
    /// nothing to keep in step with the pool. This is the cheapest way to make a prefab pool-aware.
    /// <para>
    /// Listeners are collected from the root of the pooled object the first time the pool takes ownership.
    /// A listener that throws is logged and skipped; it cannot leave the object half-returned.
    /// </para>
    /// </summary>
    public interface IPooledObjectListener
    {
        /// <summary>
        /// The pool has handed the object out, on reuse as well as first instantiation. The handle is good
        /// for the lifetime of the object, so it is safe to keep.
        /// </summary>
        public void OnAcquired(IPooledObjectHandle handle);

        /// <summary>
        /// The object is going home. Still active here, so this is where to stop work and reset state.
        /// </summary>
        public void OnReturningToPool();

        /// <summary>The object is deactivated and back in the pool.</summary>
        public void OnReturnedToPool();

        /// <summary>The object is about to be destroyed. Last chance to release anything it owns.</summary>
        public void OnDestroying();
    }
}
