using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    [Serializable]
    public class ComponentObjectPool<T> : ObjectPool where T : Component
    {
        public T Template => _template;
        [SerializeField] private T _template = null;

        private readonly Dictionary<GameObject, T> _componentMap;

        /// <summary>
        /// Every component this pool has built, lent out or parked. A fresh list each time, so acquiring
        /// and returning while iterating is safe. <see cref="GetAllComponents"/> avoids the allocation.
        /// </summary>
        public IReadOnlyList<T> AllComponents
        {
            get
            {
                List<T> results = new (_componentMap.Count);
                GetAllComponents(results);
                return results;
            }
        }

        /// <summary>Components currently lent out. A fresh list - see <see cref="AllComponents"/>.</summary>
        public IReadOnlyList<T> ActiveComponents
        {
            get
            {
                List<T> results = new (ActiveObjects.Count);
                GetActiveComponents(results);
                return results;
            }
        }

        /// <summary>Components currently parked. A fresh list - see <see cref="AllComponents"/>.</summary>
        public IReadOnlyList<T> InactiveComponents
        {
            get
            {
                List<T> results = new (InactiveObjects.Count);
                GetInactiveComponents(results);
                return results;
            }
        }

        public ComponentObjectPool() : base()
        {
            _componentMap = new ();
        }
        
        public ComponentObjectPool(int capacity) : base(capacity)
        {
            _componentMap = new (capacity);
        }
        
        public ComponentObjectPool(T template, int capacity) : base(capacity)
        {
            _template = template;
            _componentMap = new (capacity);
        }

        /// <summary>
        /// Hands out a component, building one if nothing is parked. Null if a callback sent the object
        /// back to the pool or destroyed it while Get was running - there is nothing to hand over in that
        /// case, and the pool logs the fault. A callback that took ownership through RemoveFromPool still
        /// gets its component, matching what the base pool does with the object it belongs to.
        /// </summary>
        public T Get(Action<T> onInstantiate = null)
        {
            // TryGetValue on the way in as well as the way out. Acquire runs listener code before this
            // callback, and a listener that disowns the object prunes the map from under it.
            GameObject gameObject = Get(_template.gameObject, instance =>
            {
                if (onInstantiate != null && _componentMap.TryGetValue(instance, out T instantiated))
                {
                    onInstantiate.Invoke(instantiated);
                }
            });

            // Null when a callback sent the new object straight back or destroyed it; the base has
            // already reported that. Indexing the map with it would only turn one logged fault into a
            // throw.
            if (gameObject == null)
            {
                return null;
            }

            // The map entry goes with the ownership, so a callback that disowned the object leaves nothing
            // to look up. The base treats that as a legitimate hand-off and returns the object, so this
            // has to hand over the component rather than silently drop it - resolved off the object
            // itself, since the record is gone by design rather than by accident.
            return _componentMap.TryGetValue(gameObject, out T component)
                ? component
                : gameObject.GetComponent<T>();
        }

        /// <summary>
        /// Builds instances until the pool holds at least <paramref name="count"/>, parked and ready.
        /// Call it where a hitch does not matter - a loading screen, a mini-game setting up - rather than
        /// letting the first several acquisitions pay for it.
        /// </summary>
        public void Prewarm(int count)
        {
            if (_template == null)
            {
                Debug.LogError($"ComponentObjectPool<{typeof(T).Name}>: cannot prewarm without a template.");
                return;
            }
            Prewarm(_template.gameObject, count);
        }

        /// <summary>Fills <paramref name="results"/> with every component this pool has built.</summary>
        public void GetAllComponents(List<T> results)
        {
            results.Clear();

            foreach (KeyValuePair<GameObject, T> pair in _componentMap)
            {
                results.Add(pair.Value);
            }
        }

        /// <summary>Fills <paramref name="results"/> with the components currently lent out.</summary>
        public void GetActiveComponents(List<T> results)
        {
            CopyComponents(ActiveObjects, results);
        }

        /// <summary>Fills <paramref name="results"/> with the components currently parked.</summary>
        public void GetInactiveComponents(List<T> results)
        {
            CopyComponents(InactiveObjects, results);
        }

        protected override void OnInstantiate(GameObject instantiatedObject)
        {
            _componentMap.Add(instantiatedObject, instantiatedObject.GetComponent<T>());
        }

        protected override void OnRemoved(GameObject removedObject)
        {
            // Without this the map grew for the life of the process, holding destroyed components that
            // AllComponents then handed out - and the shared static pools outlive every scene.
            _componentMap.Remove(removedObject);
        }

        /// <summary>
        /// Copies the components for <paramref name="source"/> into <paramref name="results"/>, newest
        /// first. A copy, deliberately: enumerating the pool's own list let a caller that returns objects
        /// as it goes - which is exactly what hiding a set of targets does - walk off the end of a list
        /// that had shrunk under it.
        /// </summary>
        private void CopyComponents(IReadOnlyList<GameObject> source, List<T> results)
        {
            results.Clear();

            for (int i = source.Count - 1; i >= 0; i--)
            {
                if (_componentMap.TryGetValue(source[i], out T component))
                {
                    results.Add(component);
                }
            }
        }
    }
}
