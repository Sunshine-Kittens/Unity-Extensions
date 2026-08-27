using System;

namespace UnityEngine.Extension
{
    /// <summary>
    /// The pool's grip on one object, and the object's handle back. Added by the pool when it takes
    /// ownership - never by hand: the type is internal to this assembly, so there is no route to it from
    /// the editor and no script outside the package can add one.
    /// <para>
    /// Everything the pool drives is internal to this assembly, so the public surface is only
    /// <see cref="IPooledObjectHandle"/>: the members a consumer is meant to call. Prefabs extend pooling
    /// by implementing <see cref="IPooledObjectListener"/>, not by reimplementing any of this.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PooledObject : MonoBehaviour, IPooledObjectHandle
    {
        public GameObject Instance => _state == PooledObjectState.Destroyed ? null : gameObject;
        public IObjectPool Pool => _owningPool;

        public PooledObjectState State => _state;

        /// <summary>
        /// Raised once the object has gone back to its pool, whichever entry point sent it there.
        /// Subscriptions survive pooling, since the same component is handed out again on reuse, so
        /// subscribers must unsubscribe before resubscribing.
        /// </summary>
        public event Action<GameObject> ReturnedToPool;

        /// <summary>
        /// Raised as the object is destroyed, whatever destroys it: the pool, an explicit Destroy, or the
        /// scene going down.
        /// </summary>
        public event Action<GameObject> Destroyed;

        private IObjectPool _owningPool;
        private PooledObjectState _state = PooledObjectState.Detached;
        private IPooledObjectListener[] _listeners = Array.Empty<IPooledObjectListener>();

        public void ReturnToPool()
        {
            // Deliberately thin. The pool owns the whole return sequence, so calling it directly and
            // coming through here end up in exactly the same place.
            _owningPool?.ReturnToPool(gameObject);
        }

        public void Destroy()
        {
            if (_state == PooledObjectState.Destroyed)
            {
                return;
            }

            if (_owningPool != null)
            {
                _owningPool.DestroyFromPool(gameObject);
                return;
            }

            Destroying();
            Object.Destroy(gameObject);
        }

        // -----------------------------------------------------------------------------------------------
        // Driven by the owning pool. Internal on purpose: there is no way for a consumer to put the object
        // into a state the pool does not know about.
        // -----------------------------------------------------------------------------------------------

        internal void Attach(IObjectPool pool)
        {
            _owningPool = pool ?? throw new ArgumentNullException(nameof(pool));

            // Collected once, from the root, matching how ComponentObjectPool resolves its own component.
            _listeners = GetComponents<IPooledObjectListener>();
            _state = PooledObjectState.Active;
        }

        internal void Detach()
        {
            _owningPool = null;
            if (_state != PooledObjectState.Destroyed)
                _state = PooledObjectState.Detached;
        }

        internal void Acquire()
        {
            _state = PooledObjectState.Active;

            for (int i = 0; i < _listeners.Length; i++)
            {
                try { _listeners[i].OnAcquired(this); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        internal void Returning()
        {
            for (int i = 0; i < _listeners.Length; i++)
            {
                try { _listeners[i].OnReturningToPool(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        internal void Returned()
        {
            _state = PooledObjectState.Pooled;

            for (int i = 0; i < _listeners.Length; i++)
            {
                try { _listeners[i].OnReturnedToPool(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }

            ReturnedToPool?.Invoke(gameObject);
        }

        internal void Destroying()
        {
            if (_state == PooledObjectState.Destroyed)
                return;

            _state = PooledObjectState.Destroyed;
            _owningPool = null;

            for (int i = 0; i < _listeners.Length; i++)
            {
                try { _listeners[i].OnDestroying(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }

            Destroyed?.Invoke(gameObject);
        }

        private void OnDestroy()
        {
            if (_state == PooledObjectState.Destroyed)
                return;

            // Unity is taking the object down with no pool involvement: scene unload, a parent going away,
            // or a plain Destroy call. Tell the pool before going, so it is not left holding a dead entry.
            _owningPool?.RemoveFromPool(gameObject);
            Destroying();
        }
    }
}
