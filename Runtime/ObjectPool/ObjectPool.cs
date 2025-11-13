using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public abstract class ObjectPool : IObjectPool
    {
        protected IReadOnlyList<GameObject> ActiveObjects => _activeObjects;
        private readonly List<GameObject> _activeObjects;

        protected IReadOnlyList<GameObject> InactiveObjects => _inactivePool;
        private readonly List<GameObject> _inactivePool;

        private readonly Dictionary<GameObject, IPooledObjectHandle> _handleMap;
        
        protected ObjectPool()
        {
            _activeObjects = new List<GameObject>();
            _inactivePool = new List<GameObject>();
            _handleMap = new Dictionary<GameObject, IPooledObjectHandle>();
        }

        protected ObjectPool(int capacity)
        {
            _activeObjects = new List<GameObject>(capacity);
            _inactivePool = new List<GameObject>(capacity);
            _handleMap = new Dictionary<GameObject, IPooledObjectHandle>(capacity);
        }

        protected GameObject Get(GameObject template, Action<GameObject> onInstantiate = null)
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
                onInstantiate?.Invoke(gameObject);
                IPooledObjectHandle handle = gameObject.GetComponent<IPooledObjectHandle>();
                if (handle == null)
                {
                    handle = gameObject.AddComponent<PooledObjectComponent>();
                }
                handle.Init(this);
                _handleMap.Add(gameObject, handle);
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
                    throw new InvalidOperationException("Unable to remove object from a pool that it does not belong to.");
                }
            }
            
            _handleMap.Remove(gameObject);
            if (_handleMap.Count == 0)
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
            
            if (_handleMap.Remove(gameObject, out IPooledObjectHandle handle))
            {
                handle.Destroy();   
            }
            
            if (_handleMap.Count == 0)
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
            for (int i = 0; i < _activeObjects.Count; i++)
            {
                if (_handleMap.Remove(_activeObjects[i], out IPooledObjectHandle handle))
                {
                    handle.Destroy();   
                }
            }
            _activeObjects.Clear();
            
            for (int i = 0; i < _inactivePool.Count; i++)
            {
                if (_handleMap.Remove(_inactivePool[i], out IPooledObjectHandle handle))
                {
                    handle.Destroy();   
                }
            }
            _inactivePool.Clear();
            
            _handleMap.Clear();
            OnPoolEmpty();
        }
        
        protected virtual void OnInstantiate(GameObject instantiatedObject) { }

        protected virtual void OnPoolEmpty() { }
    }
}