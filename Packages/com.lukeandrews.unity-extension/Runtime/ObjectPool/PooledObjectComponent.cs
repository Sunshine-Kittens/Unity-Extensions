using System;

namespace UnityEngine.Extension
{
    public class PooledObjectComponent : MonoBehaviour, IPooledObjectHandle
    {
        private Component _owningObject = null;
        private ObjectPool _owningPool = null;

        public void Init(Component owningObject, ObjectPool owningPool)
        {
            _owningObject = owningObject;
            _owningPool = owningPool;
        }

        public void DeactivateToPool()
        {
            if (_owningPool == null)
            {
                throw new InvalidOperationException("Owning pool is invalid.");
            }

            if (_owningObject == null)
            {
                throw new InvalidOperationException("Owning object is invalid.");
            }
            _owningPool.ReturnToPool(_owningObject);
        }

        public void DestroyFromPool()
        {
            if (_owningPool == null)
            {
                throw new InvalidOperationException("Owning pool is invalid.");
            }

            if (_owningObject == null)
            {
                throw new InvalidOperationException("Owning object is invalid.");
            }
            _owningPool.DestroyFromPool(_owningObject);
        }

        private void OnDestroy()
        {
            DestroyFromPool();
        }
    }
}