namespace UnityEngine.Extension
{
    public interface IPooledObjectHandle<T> where T : Component
    {
        public void Init(T owningObject, ObjectPool<T> owningPool);

        public void DeactivateToPool();

        public void DestroyFromPool();
    }
}