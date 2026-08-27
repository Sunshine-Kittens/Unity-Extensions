namespace UnityEngine.Extension
{
    public interface IObjectPool
    {
        public int ActiveCount { get; }
        public int InactiveCount { get; }

        /// <summary>Whether this pool owns the object, lent out or parked.</summary>
        public bool Contains(GameObject gameObject);

        /// <summary>
        /// Deactivates the object and files it back in the pool, driving the whole handle flow on the way.
        /// Returns false when the object is not one this pool is currently lending out, which covers the
        /// double return rather than throwing on it.
        /// </summary>
        public bool ReturnToPool(GameObject gameObject);

        /// <summary>
        /// Gives up ownership without destroying. The handle is detached and stops pointing here, and a
        /// parked object is lifted off the pool's parking root so a later Clear cannot take it down. It
        /// comes back deactivated - activating it is the new owner's call.
        /// </summary>
        public bool RemoveFromPool(GameObject gameObject);

        /// <summary>Takes the object out of the pool and destroys it.</summary>
        public bool DestroyFromPool(GameObject gameObject);

        public void ReturnAllToPool();

        /// <summary>
        /// Drops every instance destroyed behind the pool's back and reports how many went. Rarely needed
        /// directly - returning and clearing prune as they go - but useful after a scene has taken objects
        /// the pool was still holding.
        /// </summary>
        public int Prune();

        public void Clear();
    }
}
