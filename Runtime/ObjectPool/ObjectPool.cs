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

        private GameObject _poolRoot;

        // A pool that has never held anything has nothing to announce, so the first transition worth
        // reporting is the one out of empty and back.
        private bool _notifiedEmpty = true;

        public int ActiveCount => _activeObjects.Count;
        public int InactiveCount => _inactivePool.Count;

        /// <summary>
        /// Where returned objects are parked: an inactive object created on first use and torn down by
        /// <see cref="Clear"/>. Parking them here is what stops a borrowed parent - a character bone, a
        /// UI list, a screen being closed - taking the pool's instances down with it when it goes.
        /// </summary>
        protected Transform PoolRoot
        {
            get
            {
                if (_poolRoot == null)
                {
                    _poolRoot = new GameObject($"[Pool] {GetType().Name}");
                    _poolRoot.SetActive(false);
                    if (PersistPoolRoot)
                    {
                        Object.DontDestroyOnLoad(_poolRoot);
                    }
                }
                return _poolRoot.transform;
            }
        }

        /// <summary>
        /// Whether the parking root survives a scene load. False by default, which matches a pool owned by
        /// something in the scene. Override it for a pool that outlives scenes, or its parked objects go
        /// down with the scene and have to be built again.
        /// </summary>
        protected virtual bool PersistPoolRoot => false;

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
            GameObject gameObject = TakeFromInactive();

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
                _notifiedEmpty = false;

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

        public bool Contains(GameObject gameObject)
        {
            return gameObject != null && _pooledObjects.ContainsKey(gameObject);
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
            // Deactivate first: reparenting an inactive object skips the hierarchy churn an active one
            // would cause.
            gameObject.transform.SetParent(PoolRoot, false);
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
                OnRemoved(gameObject);
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
                OnRemoved(gameObject);
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
            // Anything destroyed while out on loan is dropped first. ReturnToPool refuses a dead object,
            // which would leave its entry behind and the loop half done.
            Prune();

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

            // The backing field, not the property: asking for the root here would build one only to
            // destroy it, and Clear runs from OnDestroy where creating objects is not always allowed.
            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot);
                _poolRoot = null;
            }

            NotifyIfEmpty();
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
                OnRemoved(pooled);
                Object.Destroy(pooled);
            }
        }

        /// <summary>
        /// Drops every instance that has been destroyed behind the pool's back and reports how many went.
        /// Handles are normally removed by their own OnDestroy, so this is the net for what that misses -
        /// a domain reload, or a scene that took the parking root with it.
        /// </summary>
        public int Prune()
        {
            int removed = PruneList(_activeObjects) + PruneList(_inactivePool);

            if (removed > 0)
            {
                NotifyIfEmpty();
            }
            return removed;
        }

        private int PruneList(List<GameObject> objects)
        {
            int removed = 0;
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                GameObject pooled = objects[i];
                if (pooled != null)
                {
                    continue;
                }

                // By index: a destroyed object is awkward to match by value, and the dictionary still
                // finds it because Unity keeps the managed key's hash after destruction.
                objects.RemoveAt(i);
                _pooledObjects.Remove(pooled);
                OnRemoved(pooled);
                removed++;
            }
            return removed;
        }

        private GameObject TakeFromInactive()
        {
            while (_inactivePool.Count > 0)
            {
                int last = _inactivePool.Count - 1;
                GameObject candidate = _inactivePool[last];
                _inactivePool.RemoveAt(last);

                if (candidate == null)
                {
                    // Destroyed while parked. Popping it rather than falling through is the difference
                    // between the pool recovering and it instantiating forever behind a dead tail.
                    _pooledObjects.Remove(candidate);
                    OnRemoved(candidate);
                    continue;
                }

                // Leaves the root the way a fresh Instantiate arrives: no parent, local transform intact.
                // Get then hands back the same shape of object whichever branch produced it.
                candidate.transform.SetParent(null, false);
                return candidate;
            }
            return null;
        }

        private void NotifyIfEmpty()
        {
            // Edge, not level. This used to fire on every removal once the map was empty, and again from
            // Clear regardless - which for the addressable pools meant releasing the same handle twice.
            if (_notifiedEmpty || _pooledObjects.Count > 0)
            {
                return;
            }

            _notifiedEmpty = true;
            OnPoolEmpty();
        }

        protected virtual void OnInstantiate(GameObject instantiatedObject) { }

        /// <summary>
        /// The object has left this pool for good - removed, destroyed, or pruned. The counterpart to
        /// <see cref="OnInstantiate"/>, and where a subclass drops whatever it recorded there. The object
        /// may already be destroyed, so treat it as a key rather than something to touch.
        /// </summary>
        protected virtual void OnRemoved(GameObject removedObject) { }

        protected virtual void OnPoolEmpty() { }
    }
}
