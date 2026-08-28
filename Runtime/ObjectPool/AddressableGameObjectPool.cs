#if UNITY_ADDRESSABLES
using System;
using System.Threading.Tasks;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Extension
{
    public class AddressableGameObjectPool : AssetBackedObjectPool
    {
        private readonly string _address;

        private AsyncOperationHandle<GameObject> _assetHandle;

        public AddressableGameObjectPool(string address, int capacity) : base(capacity)
        {
            _address = address;
        }
        
        public async Awaitable<GameObject> Get(Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> onInstantiate = null)
        {
            GameObject template = await GetTemplate();
            if (template == null)
            {
                Debug.LogError($"AddressableGameObjectPool: Failed to load asset {_address}");
                return null;
            }

            GameObject gameObject = Get(template, onInstantiate);
            if (gameObject == null)
            {
                // A callback sent it back or destroyed it. Already reported by the base.
                return null;
            }

            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        /// <summary>
        /// Loads the asset if it is not loaded, then builds instances until at least
        /// <paramref name="count"/> are parked and ready. False if the asset could not be loaded.
        /// </summary>
        public async Awaitable<bool> Prewarm(int count)
        {
            GameObject template = await GetTemplate();
            if (template == null)
            {
                Debug.LogError($"AddressableGameObjectPool: Failed to load asset {_address}");
                return false;
            }

            Prewarm(template, count);
            return true;
        }

        protected override GameObject Template => _assetHandle.IsValid() ? _assetHandle.Result : null;

        protected override Task<GameObject> BeginLoad()
        {
            _assetHandle = Addressables.LoadAssetAsync<GameObject>(_address);
            return _assetHandle.Task;
        }

        protected override void ReleaseLoaded()
        {
            if (_assetHandle.IsValid())
            {
                Addressables.Release(_assetHandle);
            }

            // Reset rather than left dangling: IsValid alone does not stop a released handle being
            // released twice, and the next load has to see that there is nothing held.
            _assetHandle = default;
        }
    }
}
#endif
