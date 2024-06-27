using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    [Serializable]
    public class ObjectPool<T> where T : Component
    {
        public T template { get { return _template; } }
        [SerializeField] private T _template = null;

        private HashSet<T> _activeObjects = new HashSet<T>();
        private List<T> _inactivePool = new List<T>();

        public T Get()
        {
            T instance;
            if (_inactivePool.Count > 0)
            {
                instance = _inactivePool[_inactivePool.Count - 1];
                _inactivePool.RemoveAt(_inactivePool.Count - 1);
                _activeObjects.Add(instance);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(_template);
                IPooledObjectHandle<T> handle = instance.GetComponent<IPooledObjectHandle<T>>();
                if (handle == null)
                {
                    instance.gameObject.AddComponent<PooledObjectComponent<T>>();
                }
            }
            return instance;
        }

        public void ReturnToPool(T pooledObject)
        {
            if (!_activeObjects.Remove(pooledObject))
            {
                throw new InvalidOperationException("Unable to return object to pool it does not belong to.");
            }
            pooledObject.gameObject.SetActive(false);
            _inactivePool.Add(pooledObject);
        }

        public void DestroyFromPool(T pooledObject)
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

        public void Clear()
        {
            foreach (T pooledObject in _activeObjects)
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
