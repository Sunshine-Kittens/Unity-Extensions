using System;

namespace UnityEngine.Extension
{
    /// <summary>
    /// A pooled object as its user sees it: what it is, who owns it, and how to send it home. Every member
    /// here is one you are meant to call. The pool drives its own side through a component internal to the
    /// package, which is deliberately not part of this surface. To react to pooling rather than drive it,
    /// implement <see cref="IPooledObjectListener"/>.
    /// </summary>
    public interface IPooledObjectHandle
    {
        /// <summary>
        /// The object this handle speaks for. Follows Unity's own destroyed-object semantics, so it is
        /// still readable while <see cref="Destroyed"/> is being raised and compares equal to null once
        /// the destruction completes. <see cref="State"/> is the authoritative lifecycle signal.
        /// </summary>
        public GameObject Instance { get; }

        /// <summary>
        /// The pool that owns the object, or null when nothing does. Test this rather than the presence of
        /// a handle: a prefab can carry one and still have been instantiated outside a pool, in which case
        /// <see cref="ReturnToPool"/> has nowhere to send it.
        /// </summary>
        public IObjectPool Pool { get; }

        public PooledObjectState State { get; }

        /// <summary>
        /// Raised once the object is back in its pool, whichever entry point sent it there.
        /// <para>
        /// Scoped to one acquisition. Both events are cleared as the object goes home or is destroyed, so
        /// subscribe on each acquisition and never unsubscribe - a subscription cannot outlive the life it
        /// was made for, and cannot accumulate across reuse.
        /// </para>
        /// </summary>
        public event Action<IPooledObjectHandle> ReturnedToPool;

        /// <summary>
        /// Raised as the object is destroyed, whatever destroys it: the pool, an explicit
        /// <see cref="Destroy"/>, or the scene going down. Scoped to one acquisition, as
        /// <see cref="ReturnedToPool"/> is.
        /// </summary>
        public event Action<IPooledObjectHandle> Destroyed;

        /// <summary>Sends the object back to its pool. Does nothing if no pool owns it.</summary>
        public void ReturnToPool();

        /// <summary>Destroys the object, taking it out of its pool on the way.</summary>
        public void Destroy();
    }
}
