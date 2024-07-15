using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public abstract class ObjectPool
    {
        private HashSet<Component> _activeObjects = new HashSet<Component>();
        private List<Component> _inactivePool = new List<Component>();

        private ObjectPool() { }

        public ObjectPool(int capacity) 
        {
            _activeObjects = new HashSet<Component>(capacity);
            _inactivePool = new List<Component>(capacity);
        }

        protected T Get<T>(T template) where T : Component
        {
            T instance;
            if (_inactivePool.Count > 0)
            {
                instance = _inactivePool[_inactivePool.Count - 1] as T;
                instance.gameObject.SetActive(true);
                _inactivePool.RemoveAt(_inactivePool.Count - 1);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(template);
                IPooledObjectHandle handle = instance.GetComponent<IPooledObjectHandle>();
                if (handle == null)
                {
                    handle = instance.gameObject.AddComponent<PooledObjectComponent>();
                }
                handle.Init(instance, this);
            }
            _activeObjects.Add(instance);
            return instance;
        }

        public void ReturnToPool(Component pooledObject)
        {
            if (!_activeObjects.Remove(pooledObject))
            {
                throw new InvalidOperationException("Unable to return object to pool it does not belong to.");
            }
            pooledObject.gameObject.SetActive(false);
            _inactivePool.Add(pooledObject);
        }

        public void DestroyFromPool(Component pooledObject)
        {
            if (!_activeObjects.Remove(pooledObject))
            {
                if (!_inactivePool.Remove(pooledObject))
                {
                    throw new InvalidOperationException("Unable to destroy object from a pool that it does not belong to.");
                }
            }
            UnityEngine.Object.Destroy(pooledObject);
        }

        public void ReturnAllToPool()
        {
            foreach (Component pooledObject in _activeObjects)
            {
                pooledObject.gameObject.SetActive(false);
                _inactivePool.Add(pooledObject);
            }
            _activeObjects.Clear();
        }

        public void Clear()
        {
            foreach (Component pooledObject in _activeObjects)
            {
                UnityEngine.Object.Destroy(pooledObject);
            }
            _activeObjects.Clear();
            for (int i = 0; i < _inactivePool.Count; i++)
            {
                UnityEngine.Object.Destroy(_inactivePool[i]);
            }
            _inactivePool.Clear();
        }
    }
}