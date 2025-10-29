using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public abstract class ObjectPool
    {
        private readonly HashSet<Component> _activeObjects;
        private readonly List<Component> _inactivePool;

        protected ObjectPool()
        {
            _activeObjects = new HashSet<Component>();
            _inactivePool = new List<Component>();
        }

        protected ObjectPool(int capacity)
        {
            _activeObjects = new HashSet<Component>(capacity);
            _inactivePool = new List<Component>(capacity);
        }

        protected T Get<T>(T template) where T : Component
        {
            T instance = null;
            if (_inactivePool.Count > 0)
            {
                Component component = _inactivePool[^1];
                if (component != null)
                {
                    component.gameObject.SetActive(true);
                    _inactivePool.RemoveAt(_inactivePool.Count - 1);
                    instance = component as T;
                }
            }
            
            if(instance == null)
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

        public void RemoveFromPool(Component pooledObject)
        {
            if (!_activeObjects.Remove(pooledObject))
            {
                if (!_inactivePool.Remove(pooledObject))
                {
                    throw new InvalidOperationException("Unable to destroy object from a pool that it does not belong to.");
                }
            }
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
            Object.Destroy(pooledObject);
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
                Object.Destroy(pooledObject);
            }
            _activeObjects.Clear();
            for (int i = 0; i < _inactivePool.Count; i++)
            {
                Object.Destroy(_inactivePool[i]);
            }
            _inactivePool.Clear();
        }
    }
}