#if UNITY_ADDRESSABLES
using System;
using System.Threading.Tasks;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Extension
{
    public class AddressableGameObjectPool : ObjectPool
    {
        private readonly string _address;

        private AsyncOperationHandle<GameObject> _assetHandle;
        private Task<GameObject> _loadTask;

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
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        /// <summary>
        /// Loads the asset if it is not loaded, then builds instances until the pool holds at least
        /// <paramref name="count"/>, parked and ready. False if the asset could not be loaded.
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

        /// <summary>
        /// One load, however many callers arrive while it is in flight. Each used to start its own the
        /// moment it saw an invalid handle; every handle but the last was then overwritten and leaked.
        /// </summary>
        private async Awaitable<GameObject> GetTemplate()
        {
            if (_assetHandle.IsValid() && _assetHandle.Result != null)
                return _assetHandle.Result;

            if (_loadTask == null)
            {
                // An attempt that resolved to nothing still leaves a valid handle behind. Release it
                // before asking for another, or the failed one is overwritten and never given back -
                // the very leak the shared gate below exists to prevent.
                ReleaseAsset();

                _assetHandle = Addressables.LoadAssetAsync<GameObject>(_address);
                _loadTask = _assetHandle.Task;
            }

            try
            {
                return await _loadTask;
            }
            finally
            {
                // Cleared by whichever caller finishes first; the rest are no-ops. A failed load then
                // leaves the gate open so the next Get can try again.
                _loadTask = null;
            }
        }

        protected override void OnPoolEmpty()
        {
            ReleaseAsset();
        }

        private void ReleaseAsset()
        {
            _loadTask = null;

            if (_assetHandle.IsValid())
                Addressables.Release(_assetHandle);

            // Reset rather than left dangling: IsValid alone does not stop a released handle being
            // released twice, and the next load has to see that there is nothing held.
            _assetHandle = default;
        }
    }
}
#endif
