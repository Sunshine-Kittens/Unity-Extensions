using System;

namespace UnityEngine.Extension
{
    [Serializable]
    public class ObjectPoolT<T> : ObjectPool where T : Component
    {
        public ObjectPoolT(int capacity) : base(capacity) { }

        public T template { get { return _template; } }
        [SerializeField] private T _template = null;

        public T Get()
        {
            return Get(_template);
        }
    }
}