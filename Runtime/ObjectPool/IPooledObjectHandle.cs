namespace UnityEngine.Extension
{
    public interface IPooledObjectHandle
    {
        public void Init(Component owningObject, ObjectPool owningPool);

        public void ReturnToPool();

        public void DestroyFromPool();
    }
}