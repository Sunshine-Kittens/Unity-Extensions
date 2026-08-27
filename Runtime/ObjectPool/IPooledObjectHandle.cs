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
        /// <summary>The object this handle speaks for. Null once destroyed.</summary>
        public GameObject Instance { get; }

        /// <summary>
        /// The pool that owns the object, or null when nothing does. Test this rather than the presence of
        /// a handle: a prefab can carry one and still have been instantiated outside a pool, in which case
        /// <see cref="ReturnToPool"/> has nowhere to send it.
        /// </summary>
        public IObjectPool Pool { get; }

        public PooledObjectState State { get; }

        //TODO: Replace the GameObject payload with IPooledObjectHandle now that the interface exposes the
        //object it manages. Deferred because it breaks every subscriber signature in the consuming app,
        //which makes it a two-repository change rather than a package one.

        /// <summary>
        /// Raised once the object is back in its pool, whichever entry point sent it there. Subscriptions
        /// survive pooling, since the same handle is handed out again on reuse, so subscribers must
        /// unsubscribe before resubscribing.
        /// </summary>
        public event Action<GameObject> ReturnedToPool;

        /// <summary>
        /// Raised as the object is destroyed, whatever destroys it: the pool, an explicit
        /// <see cref="Destroy"/>, or the scene going down.
        /// </summary>
        public event Action<GameObject> Destroyed;

        /// <summary>Sends the object back to its pool. Does nothing if no pool owns it.</summary>
        public void ReturnToPool();

        /// <summary>Destroys the object, taking it out of its pool on the way.</summary>
        public void Destroy();
    }
}
