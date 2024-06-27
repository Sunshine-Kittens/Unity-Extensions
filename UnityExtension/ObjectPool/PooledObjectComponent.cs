namespace UnityEngine.Extension
{
    public class PooledObjectComponent<T> : MonoBehaviour, IPooledObjectHandle<T> where T : Component
    {
        private T _owningObject = null;
        private ObjectPool<T> _owningPool = null;

        public void Init(T owningObject, ObjectPool<T> owningPool)
        {
            _owningObject = owningObject;
            _owningPool = owningPool;
        }

        public void DeactivateToPool()
        {
            _owningPool.ReturnToPool(_owningObject);
        }

        public void DestroyFromPool()
        {
            _owningPool.DestroyFromPool(_owningObject);
        }

        private void OnDestroy()
        {
            DestroyFromPool();
        }
    }
}
