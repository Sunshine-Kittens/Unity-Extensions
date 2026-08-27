#if UNITY_ADDRESSABLES
using System;
using System.Threading.Tasks;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Extension
{
    [Serializable]
    public class AssetReferenceGameObjectPool : ObjectPool
    {
        [SerializeField] private AssetReferenceGameObject _assetReference;
        
        private AsyncOperationHandle<GameObject> _assetHandle;
        private Task<GameObject> _loadTask;

        // Unity builds a serialized field through the parameterless constructor. Without one this type
        // could not round-trip at all, which is the only reason it has never been used from the inspector.
        public AssetReferenceGameObjectPool() { }

        public AssetReferenceGameObjectPool(int capacity) : base(capacity) { }

        public async Awaitable<GameObject> Get(Action<GameObject> onInstantiate = null)
        {
            GameObject template = await GetTemplate();
            if (template == null)
            {
                Debug.LogError($"AssetReferenceGameObjectPool: Failed to load {_assetReference}");
                return null;
            }
            return Get(template, onInstantiate);
        }

        /// <summary>One load, however many callers arrive while it is in flight.</summary>
        private async Awaitable<GameObject> GetTemplate()
        {
            // IsValid first: reading Result on a default handle throws rather than returning null, which
            // is what the old null check walked straight into.
            if (_assetHandle.IsValid() && _assetHandle.Result != null)
                return _assetHandle.Result;

            if (_assetReference == null || !_assetReference.RuntimeKeyIsValid())
                return null;

            if (_loadTask == null)
            {
                _assetHandle = _assetReference.LoadAssetAsync();
                _loadTask = _assetHandle.Task;
            }

            try
            {
                return await _loadTask;
            }
            finally
            {
                _loadTask = null;
            }
        }

        protected override void OnPoolEmpty()
        {
            _loadTask = null;

            if (_assetHandle.IsValid())
                Addressables.Release(_assetHandle);
            
            _assetHandle = default;
        }
    }
}
#endif
