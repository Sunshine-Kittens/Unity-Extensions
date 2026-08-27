using System;

namespace UnityEngine.Extension
{
    public class PooledObjectComponent : MonoBehaviour, IPooledObjectHandle
    {
        /// <summary>
        /// Raised once the object has gone back to its pool. Subscriptions survive pooling, since the same
        /// component is handed out again on reuse, so subscribers must unsubscribe before resubscribing.
        /// </summary>
        public event Action<GameObject> ReturnedToPool;

        /// <summary>
        /// Raised as the object is destroyed, whatever destroys it: the pool, an explicit Destroy, or the
        /// scene going down. A subclass overriding OnDestroy must call base.OnDestroy() or it never fires.
        /// </summary>
        public event Action<GameObject> Destroyed;

        private bool _pendingDestroy = false;
        private IObjectPool _owningPool = null;

        public void Init(IObjectPool owningPool)
        {
            _owningPool = owningPool ?? throw new ArgumentNullException(nameof(owningPool));
        }

        public void ReturnToPool()
        {
            if (_owningPool != null)
            {
                OnReturnToPool();
                _owningPool.ReturnToPool(gameObject);
                ReturnedToPool?.Invoke(gameObject);
            }
        }

        protected virtual void OnReturnToPool() { }

        public void DestroyFromPool()
        {
            if (!_pendingDestroy && _owningPool != null)
            {
                _pendingDestroy = true;
                _owningPool.DestroyFromPool(gameObject);
            }
        }

        public void Destroy()
        {
            if (!_pendingDestroy)
            {
                _pendingDestroy = true;
                Destroy(gameObject);
            }
        }
        
        protected virtual void OnDestroy()
        {
            DestroyFromPool();
            Destroyed?.Invoke(gameObject);
        }
    }
}