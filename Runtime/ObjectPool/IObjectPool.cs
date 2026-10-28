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
        /// <para>
        /// False means the object did not reach the parked set, which covers two cases: it was not one
        /// this pool was lending out - a double return, rather than a throw - or a listener destroyed or
        /// disowned it while it was on its way home.
        /// </para>
        /// </summary>
        public bool ReturnToPool(GameObject gameObject);

        /// <summary>
        /// Gives up ownership without destroying. The handle is detached and stops pointing here, and a
        /// parked object is lifted off the pool's parking root so a later Clear cannot take it down. The
        /// object is handed back exactly as it was - one that was parked is still deactivated, one that
        /// was lent out is still live. The pool changes no active state on the way out.
        /// <para>
        /// The object stays the caller's problem from here, and a pool backed by a loaded asset keeps
        /// that asset for as long as the object it gave up is alive - it is still an instance of it.
        /// </para>
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
