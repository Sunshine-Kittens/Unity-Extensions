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

        private readonly Dictionary<GameObject, PooledObject> _pooledObjects;

        public int ActiveCount => _activeObjects.Count;
        public int InactiveCount => _inactivePool.Count;

        protected ObjectPool()
        {
            _activeObjects = new List<GameObject>();
            _inactivePool = new List<GameObject>();
            _pooledObjects = new Dictionary<GameObject, PooledObject>();
        }

        protected ObjectPool(int capacity)
        {
            _activeObjects = new List<GameObject>(capacity);
            _inactivePool = new List<GameObject>(capacity);
            _pooledObjects = new Dictionary<GameObject, PooledObject>(capacity);
        }

        protected GameObject Get(GameObject template, Action<GameObject> onInstantiate = null)
        {
            GameObject gameObject = null;
            if (_inactivePool.Count > 0)
            {
                gameObject = _inactivePool[^1];
                if (gameObject != null)
                {
                    _inactivePool.RemoveAt(_inactivePool.Count - 1);
                }
            }

            if(gameObject == null)
            {
                gameObject = Object.Instantiate(template);
                OnInstantiate(gameObject);

                PooledObject newPooledObject = gameObject.GetComponent<PooledObject>();
                if (newPooledObject == null)
                    newPooledObject = gameObject.AddComponent<PooledObject>();

                // Ownership is settled before the callback runs. An unowned handle ignores ReturnToPool,
                // so anything onInstantiate did to send the object straight home was dropped.
                newPooledObject.Attach(this);
                _pooledObjects[gameObject] = newPooledObject;

                onInstantiate?.Invoke(gameObject);
            }

            // Both branches, not just reuse: a prefab is free to deactivate itself in Awake, and the pool
            // still owes the caller something live.
            gameObject.SetActive(true);
            _activeObjects.Add(gameObject);

            if (_pooledObjects.TryGetValue(gameObject, out PooledObject pooledObject))
            {
                pooledObject.Acquire();
            }
            return gameObject;
        }

        public bool ReturnToPool(GameObject gameObject)
        {
            if (gameObject == null || !_activeObjects.Remove(gameObject))
            {
                return false;
            }

            _pooledObjects.TryGetValue(gameObject, out PooledObject pooledObject);

            pooledObject?.Returning();
            gameObject.SetActive(false);
            _inactivePool.Add(gameObject);
            pooledObject?.Returned();
            return true;
        }

        public bool RemoveFromPool(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            // Both removals run: | rather than || is deliberate.
            bool tracked = _activeObjects.Remove(gameObject) | _inactivePool.Remove(gameObject);

            if (_pooledObjects.Remove(gameObject, out PooledObject pooledObject))
            {
                // Ownership is given up on both sides. A handle left pointing at a pool that no longer
                // knows it calls back in on destruction and finds itself in neither list.
                pooledObject.Detach();
                tracked = true;
            }

            if (tracked)
            {
                NotifyIfEmpty();
            }
            return tracked;
        }

        public bool DestroyFromPool(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            bool tracked = _activeObjects.Remove(gameObject) | _inactivePool.Remove(gameObject);

            if (_pooledObjects.Remove(gameObject, out PooledObject pooledObject))
            {
                pooledObject.Destroying();
                tracked = true;
            }

            Object.Destroy(gameObject);

            if (tracked)
            {
                NotifyIfEmpty();
            }
            return tracked;
        }

        public void ReturnAllToPool()
        {
            if (_activeObjects.Count == 0)
            {
                return;
            }

            // Snapshot: Returned runs listener and subscriber code, and anything that returns or acquires
            // another object would reshape the list underneath the loop.
            GameObject[] returning = _activeObjects.ToArray();
            for (int i = 0; i < returning.Length; i++)
            {
                ReturnToPool(returning[i]);
            }
        }

        public void Clear()
        {
            DestroyAll(_activeObjects);
            DestroyAll(_inactivePool);

            _pooledObjects.Clear();
            OnPoolEmpty();
        }

        private void DestroyAll(List<GameObject> objects)
        {
            if (objects.Count == 0)
            {
                return;
            }

            GameObject[] destroying = objects.ToArray();
            objects.Clear();

            for (int i = 0; i < destroying.Length; i++)
            {
                GameObject pooled = destroying[i];
                if (pooled == null)
                {
                    continue;
                }

                if (_pooledObjects.Remove(pooled, out PooledObject pooledObject))
                {
                    pooledObject.Destroying();
                }
                Object.Destroy(pooled);
            }
        }

        private void NotifyIfEmpty()
        {
            if (_pooledObjects.Count == 0)
            {
                OnPoolEmpty();
            }
        }

        protected virtual void OnInstantiate(GameObject instantiatedObject) { }

        protected virtual void OnPoolEmpty() { }
    }
}
