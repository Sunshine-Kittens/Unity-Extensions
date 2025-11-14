using System;

namespace UnityEngine.Extension
{
    public class PooledObjectComponent : MonoBehaviour, IPooledObjectHandle
    {
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
        }
    }
}