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

        public T Get(Action<T> onInstantiate = null)
        {
            GameObject gameObject = Get(_template.gameObject, 
                instance => onInstantiate?.Invoke(_componentMap[instance])
            );

            // Null when the instantiate callback sent the new object straight back; the base has already
            // reported that. Indexing the map with it would only turn one logged fault into a throw.
            return gameObject != null && _componentMap.TryGetValue(gameObject, out T component)
                ? component
                : null;
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
