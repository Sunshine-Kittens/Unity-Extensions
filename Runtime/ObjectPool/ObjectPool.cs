using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public abstract class ObjectPool : IObjectPool
    {
        private readonly HashSet<GameObject> _activeObjects;
        private readonly List<GameObject> _inactivePool;

        protected ObjectPool()
        {
            _activeObjects = new HashSet<GameObject>();
            _inactivePool = new List<GameObject>();
        }

        protected ObjectPool(int capacity)
        {
            _activeObjects = new HashSet<GameObject>(capacity);
            _inactivePool = new List<GameObject>(capacity);
        }

        protected GameObject Get(GameObject template)
        {
            GameObject gameObject = null;
            if (_inactivePool.Count > 0)
            {
                gameObject = _inactivePool[^1];
                if (gameObject != null)
                {
                    gameObject.SetActive(true);
                    _inactivePool.RemoveAt(_inactivePool.Count - 1);
                }
            }
            
            if(gameObject == null)
            {
                gameObject = Object.Instantiate(template);
                OnInstantiate(gameObject);
                IPooledObjectHandle handle = gameObject.GetComponent<IPooledObjectHandle>();
                if (handle == null)
                {
                    handle = gameObject.AddComponent<PooledObjectComponent>();
                }
                handle.Init(this);
            }
            _activeObjects.Add(gameObject);
            return gameObject;
        }

        public void ReturnToPool(GameObject gameObject)
        {
            if (!_activeObjects.Remove(gameObject))
            {
                throw new InvalidOperationException("Unable to return object to pool it does not belong to.");
            }
            gameObject.SetActive(false);
            _inactivePool.Add(gameObject);
        }

        public void RemoveFromPool(GameObject gameObject)
        {
            if (!_activeObjects.Remove(gameObject))
            {
                if (!_inactivePool.Remove(gameObject))
                {
                    throw new InvalidOperationException("Unable to destroy object from a pool that it does not belong to.");
                }
            }
            
            if (_activeObjects.Count == 0 &&  _inactivePool.Count == 0)
            {
                OnPoolEmpty();
            }
        }
        
        public void DestroyFromPool(GameObject gameObject)
        {
            if (!_activeObjects.Remove(gameObject))
            {
                if (!_inactivePool.Remove(gameObject))
                {
                    throw new InvalidOperationException("Unable to destroy object from a pool that it does not belong to.");
                }
            }
            Object.Destroy(gameObject);

            if (_activeObjects.Count == 0 &&  _inactivePool.Count == 0)
            {
                OnPoolEmpty();
            }
        }

        public void ReturnAllToPool()
        {
            foreach (GameObject pooledObject in _activeObjects)
            {
                pooledObject.SetActive(false);
                _inactivePool.Add(pooledObject);
            }
            _activeObjects.Clear();
        }

        public void Clear()
        {
            foreach (GameObject pooledObject in _activeObjects)
            {
                Object.Destroy(pooledObject);
            }
            _activeObjects.Clear();
            for (int i = 0; i < _inactivePool.Count; i++)
            {
                Object.Destroy(_inactivePool[i]);
            }
            _inactivePool.Clear();
            OnPoolEmpty();
        }
        
        protected virtual void OnInstantiate(GameObject instantiatedObject) { }

        protected virtual void OnPoolEmpty() { }
    }
}