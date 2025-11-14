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

        public IEnumerable<T> AllComponents
        {
            get
            {
                foreach (var pair in _componentMap)
                    yield return pair.Value;
            }
        }
        
        public IEnumerable<T> ActiveComponents
        {
            get
            {
                for (int i = ActiveObjects.Count - 1; i >= 0; i--)
                {
                    yield return _componentMap[ActiveObjects[i]];   
                }
            }
        }
        
        public IEnumerable<T> InactiveComponents
        {
            get
            {
                for (int i = InactiveObjects.Count - 1; i >= 0; i--)
                {
                    yield return _componentMap[InactiveObjects[i]];   
                }
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
                gameObject => onInstantiate?.Invoke(_componentMap[gameObject])
            );
            return _componentMap[gameObject];
        }

        protected override void OnInstantiate(GameObject instantiatedObject)
        {
            _componentMap.Add(instantiatedObject, instantiatedObject.GetComponent<T>());
        }
    }
}