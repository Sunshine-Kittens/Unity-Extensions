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

        [Tooltip("Instances to reserve list space for. Serialized so an inspector value survives - the " +
                 "constructor argument does not, because Unity rebuilds a serialized pool with the " +
                 "parameterless one. Reserves space only; use Prewarm to build the instances up front.")]
        [SerializeField] private int _capacity;

        [Tooltip("Most instances to keep parked. Anything returned past this is destroyed rather than " +
                 "kept. Zero is unbounded.")]
        [SerializeField] private int _maxParked;

        private GameObject _poolRoot;
        private bool _capacityReserved;

        // A pool that has never held anything has nothing to announce, so the first transition worth
        // reporting is the one out of empty and back.
        private bool _notifiedEmpty = true;

        public int ActiveCount => _activeObjects.Count;
        public int InactiveCount => _inactivePool.Count;

        /// <summary>Instances built over this pool's life. Still climbing means it is not being reused.</summary>
        public int InstantiatedCount { get; private set; }

        /// <summary>Acquisitions served from the parked set rather than by building something.</summary>
        public int ReusedCount { get; private set; }

        /// <summary>The most instances lent out at once - the size this pool actually needs.</summary>
        public int PeakActiveCount { get; private set; }

        /// <summary>
        /// Most instances to keep parked; anything returned past it is destroyed rather than kept. Zero,
        /// the default, is unbounded. Worth setting for a pool that spikes well above its steady demand.
        /// </summary>
        public int MaxParked
        {
            get => _maxParked;
            set => _maxParked = value < 0 ? 0 : value;
        }

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
            _capacity = capacity;
            _activeObjects = new List<GameObject>(capacity);
            _inactivePool = new List<GameObject>(capacity);
            _pooledObjects = new Dictionary<GameObject, PooledObject>(capacity);
        }

        protected GameObject Get(GameObject template, Action<GameObject> onInstantiate = null)
        {
            ReserveCapacity();

            GameObject gameObject = TakeFromInactive();

            if(gameObject == null)
            {
                gameObject = CreateInstance(template, onInstantiate);
            }
            else
            {
                ReusedCount++;
            }

            // Both branches, not just reuse: a prefab is free to deactivate itself in Awake, and the pool
            // still owes the caller something live.
            gameObject.SetActive(true);
            _activeObjects.Add(gameObject);

            if (_activeObjects.Count > PeakActiveCount)
            {
                PeakActiveCount = _activeObjects.Count;
            }

            if (_pooledObjects.TryGetValue(gameObject, out PooledObject pooledObject))
            {
                pooledObject.Acquire();
            }
            return gameObject;
        }

        /// <summary>
        /// Builds instances until the pool holds at least <paramref name="count"/>, parking them ready to
        /// hand out. Moves the instantiation cost to a moment of your choosing rather than the first
        /// several acquisitions.
        /// </summary>
        protected void Prewarm(GameObject template, int count)
        {
            if (template == null || count <= 0)
            {
                return;
            }

            ReserveCapacity();

            while (_pooledObjects.Count < count)
            {
                if (_maxParked > 0 && _inactivePool.Count >= _maxParked)
                {
                    // Parking past the ceiling only means destroying it again on the first return.
                    break;
                }

                GameObject instance = CreateInstance(template, null);

                // Parked directly rather than through the return path: nothing acquired it, so there is
                // no acquisition to announce the end of.
                instance.SetActive(false);
                instance.transform.SetParent(PoolRoot, false);
                _inactivePool.Add(instance);
            }
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

            if (_maxParked > 0 && _inactivePool.Count >= _maxParked)
            {
                // Past the ceiling. The object is told it is going home and then destroyed instead of
                // parked, so a listener still gets its stop-work call and observers still hear it leave.
                DestroyFromPool(gameObject);
                return true;
            }

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

            bool tracked = _activeObjects.Remove(gameObject)
                | _inactivePool.Remove(gameObject)
                | _pooledObjects.ContainsKey(gameObject);

            if (!tracked)
            {
                // Not ours. Destroying it anyway would make this method a way to destroy any object at
                // all, which is not what a pool is for.
                return false;
            }

            DestroyInstance(gameObject);
            NotifyIfEmpty();
            return true;
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

        /// <summary>
        /// Destroys parked instances until at most <paramref name="keep"/> remain, and reports how many
        /// went. For releasing the tail of a spike without tearing down the pool.
        /// </summary>
        public int Trim(int keep)
        {
            if (keep < 0)
            {
                keep = 0;
            }

            int destroyed = 0;
            while (_inactivePool.Count > keep)
            {
                int last = _inactivePool.Count - 1;
                GameObject parked = _inactivePool[last];
                _inactivePool.RemoveAt(last);

                DestroyInstance(parked);
                destroyed++;
            }

            if (destroyed > 0)
            {
                NotifyIfEmpty();
            }
            return destroyed;
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

        private GameObject CreateInstance(GameObject template, Action<GameObject> onInstantiate)
        {
            GameObject instance = Object.Instantiate(template);
            InstantiatedCount++;
            OnInstantiate(instance);

            PooledObject pooledObject = instance.GetComponent<PooledObject>();
            if (pooledObject == null)
                pooledObject = instance.AddComponent<PooledObject>();

            // Ownership is settled before the callback runs. An unowned handle ignores ReturnToPool, so
            // anything onInstantiate did to send the object straight home was dropped.
            pooledObject.Attach(this);
            _pooledObjects[instance] = pooledObject;
            _notifiedEmpty = false;

            onInstantiate?.Invoke(instance);
            return instance;
        }

        private void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (_pooledObjects.Remove(instance, out PooledObject pooledObject))
            {
                pooledObject.Destroying();
            }

            OnRemoved(instance);
            Object.Destroy(instance);
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
                DestroyInstance(destroying[i]);
            }
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

        // Deferred rather than done in the constructor: Unity rebuilds a serialized pool through the
        // parameterless one and only then assigns _capacity, so there is nothing to reserve until now.
        private void ReserveCapacity()
        {
            if (_capacityReserved)
            {
                return;
            }
            _capacityReserved = true;

            if (_capacity <= 0)
            {
                return;
            }

            if (_activeObjects.Capacity < _capacity)
            {
                _activeObjects.Capacity = _capacity;
            }
            if (_inactivePool.Capacity < _capacity)
            {
                _inactivePool.Capacity = _capacity;
            }
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
