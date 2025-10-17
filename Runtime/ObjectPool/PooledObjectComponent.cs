using System;

namespace UnityEngine.Extension
{
    public class PooledObjectComponent : MonoBehaviour, IPooledObjectHandle
    {
        private Component _owningObject = null;
        private ObjectPool _owningPool = null;

        public void Init(Component owningObject, ObjectPool owningPool)
        {
            _owningObject = owningObject ?? throw new ArgumentNullException(nameof(owningObject));
            _owningPool = owningPool ?? throw new ArgumentNullException(nameof(owningPool));
        }

        public void ReturnToPool()
        {
            if (_owningPool != null && _owningObject != null)
            {
                OnReturnToPool();
                _owningPool.ReturnToPool(_owningObject);
            }
        }

        protected virtual void OnReturnToPool() { }

        public void DestroyFromPool()
        {
            if (_owningPool != null && _owningObject != null)
            {
                _owningPool.DestroyFromPool(_owningObject);
            }
        }

        protected virtual void OnDestroy()
        {
            DestroyFromPool();
        }
    }
}