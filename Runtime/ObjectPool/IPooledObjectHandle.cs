using System;

namespace UnityEngine.Extension
{
    public interface IPooledObjectHandle
    {
        //TODO: Replace the GameObject payload with IPooledObjectHandle once the interface exposes the
        //object it manages; subscribers match on the GameObject today, which the handle cannot supply.
        public event Action<GameObject> ReturnedToPool;
        public event Action<GameObject> Destroyed;
        
        public void Init(IObjectPool owningPool);
        public void ReturnToPool();
        public void Destroy();
        public void DestroyFromPool();
    }
}