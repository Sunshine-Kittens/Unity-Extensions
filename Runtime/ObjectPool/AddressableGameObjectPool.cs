#if UNITY_ADDRESSABLES
using System;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Extension
{
    public class AddressableGameObjectPool : ObjectPool
    {
        private AsyncOperationHandle<GameObject> _assetHandle;
        private readonly string _address;

        public AddressableGameObjectPool(string address, int capacity) : base(capacity)
        {
            _address = address;
        }
        
        public async Awaitable<GameObject> Get(Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> onInstantiate = null)
        {
            if (!_assetHandle.IsValid())
            {
                _assetHandle = Addressables.LoadAssetAsync<GameObject>(_address);
                await _assetHandle.Task;
            }
            GameObject gameObject = Get(_assetHandle.Result, onInstantiate);
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        protected override void OnPoolEmpty()
        {
            Addressables.Release(_assetHandle);
        }
    }
}
#endif