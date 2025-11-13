namespace UnityEngine.Extension
{
    public interface IPooledObjectHandle
    {
        public void Init(IObjectPool owningPool);
        public void ReturnToPool();
        public void Destroy();
        public void DestroyFromPool();
    }
}