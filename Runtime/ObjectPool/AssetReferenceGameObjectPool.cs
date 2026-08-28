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

        // Bumped by every release. A load that was in flight when the pool was cleared resolves against a
        // handle that has already been given back, so its result has to be discarded rather than handed
        // out - and the comparison is the only way to tell, since the await itself completes normally.
        private int _loadGeneration;

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
                // A previous attempt that resolved to nothing still holds a handle - release it rather
                // than assign over it and lose the reference.
                ReleaseAsset();

                _assetHandle = _assetReference.LoadAssetAsync();
                _loadTask = _assetHandle.Task;
            }

            int generation = _loadGeneration;

            try
            {
                GameObject loaded = await _loadTask;
                return generation == _loadGeneration ? loaded : null;
            }
            finally
            {
                // Left alone if a release happened meanwhile: that already cleared the gate and opened a
                // new generation, and clearing it again would discard a newer load's task.
                if (generation == _loadGeneration)
                {
                    _loadTask = null;
                }
            }
        }

        protected override void OnPoolEmpty()
        {
            ReleaseAsset();
        }

        private void ReleaseAsset()
        {
            _loadGeneration++;
            _loadTask = null;

            if (_assetHandle.IsValid())
                Addressables.Release(_assetHandle);

            _assetHandle = default;
        }
    }
}
#endif
