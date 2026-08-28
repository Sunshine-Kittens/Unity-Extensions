#if UNITY_ADDRESSABLES
using System;
using System.Threading.Tasks;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Extension
{
    [Serializable]
    public class AssetReferenceGameObjectPool : AssetBackedObjectPool
    {
        [SerializeField] private AssetReferenceGameObject _assetReference;
        
        private AsyncOperationHandle<GameObject> _assetHandle;

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

        protected override GameObject Template => _assetHandle.IsValid() ? _assetHandle.Result : null;

        protected override Task<GameObject> BeginLoad()
        {
            if (_assetReference == null || !_assetReference.RuntimeKeyIsValid())
            {
                return null;
            }

            _assetHandle = _assetReference.LoadAssetAsync();
            return _assetHandle.Task;
        }

        protected override void ReleaseLoaded()
        {
            // The AssetReference's own release, not Addressables.Release: only this clears the cached
            // operation. Releasing an in-flight load the other way leaves the reference believing it is
            // still loaded, and the next load gets a default handle whose Task throws.
            if (_assetReference != null && _assetHandle.IsValid())
            {
                _assetReference.ReleaseAsset();
            }

            _assetHandle = default;
        }
    }
}
#endif
