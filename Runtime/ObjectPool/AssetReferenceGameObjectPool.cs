#if UNITY_ADDRESSABLES
using System;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Extension
{
    [Serializable]
    public class AssetReferenceGameObjectPool : ObjectPool
    {
        [SerializeField] private AssetReferenceGameObject _assetReference;
        
        private AsyncOperationHandle<GameObject> _assetHandle;

        public AssetReferenceGameObjectPool(int capacity) : base(capacity) { }

        public async Awaitable<GameObject> Get(Action<GameObject> onInstantiate = null)
        {
            if (_assetHandle.Result == null)
            {
                _assetHandle = _assetReference.LoadAssetAsync();
                await _assetHandle.Task;
            }
            return Get(_assetHandle.Result, onInstantiate);
        }

        protected override void OnPoolEmpty()
        {
            Addressables.Release(_assetHandle);
        }
    }
}
#endif