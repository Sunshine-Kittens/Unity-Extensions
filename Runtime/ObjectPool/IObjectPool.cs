namespace UnityEngine.Extension
{
    public interface IObjectPool
    {
        public void ReturnToPool(GameObject gameObject);
        public void RemoveFromPool(GameObject gameObject);
        public void DestroyFromPool(GameObject gameObject);
        public void ReturnAllToPool();
        public void Clear();
    }
}