using System;

namespace UnityEngine.Extension
{
    public class PooledObjectComponent : MonoBehaviour, IPooledObjectHandle
    {
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
            if (_owningPool != null)
            {
                _owningPool.DestroyFromPool(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            DestroyFromPool();
        }
    }
}