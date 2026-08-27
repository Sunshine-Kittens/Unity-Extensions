namespace UnityEngine.Extension
{
    public interface IObjectPool
    {
        public int ActiveCount { get; }
        public int InactiveCount { get; }

        /// <summary>
        /// Deactivates the object and files it back in the pool, driving the whole handle flow on the way.
        /// Returns false when the object is not one this pool is currently lending out, which covers the
        /// double return rather than throwing on it.
        /// </summary>
        public bool ReturnToPool(GameObject gameObject);

        /// <summary>Gives up ownership without destroying. The handle is detached and stops pointing here.</summary>
        public bool RemoveFromPool(GameObject gameObject);

        /// <summary>Takes the object out of the pool and destroys it.</summary>
        public bool DestroyFromPool(GameObject gameObject);

        public void ReturnAllToPool();
        public void Clear();
    }
}
