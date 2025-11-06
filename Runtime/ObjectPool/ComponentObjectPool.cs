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
        }

        public T Get()
        {
            GameObject gameObject = Get(_template.gameObject);
            return _componentMap[gameObject];
        }

        protected override void OnInstantiate(GameObject instantiatedObject)
        {
            _componentMap.Add(instantiatedObject, instantiatedObject.GetComponent<T>());
        }
    }
}