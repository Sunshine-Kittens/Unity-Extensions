using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace UnityEngine.Extension.Tests
{
    /// <summary>
    /// Shared harness. ObjectPool is abstract and its acquisition is protected, so the tests drive it
    /// through a concrete pool that also records the two hooks a subclass is given.
    /// </summary>
    public abstract class ObjectPoolTestFixture
    {
        protected sealed class TestPool : ObjectPool
        {
            private readonly GameObject _template;

            public int PoolEmptyCount { get; private set; }
            public int RemovedCount { get; private set; }
            public int InstantiateHookCount { get; private set; }

            public TestPool(GameObject template, int capacity = 0) : base(capacity)
            {
                _template = template;
            }

            public GameObject Acquire(Action<GameObject> onInstantiate = null) => Get(_template, onInstantiate);

            public void Fill(int count) => Prewarm(_template, count);

            protected override void OnInstantiate(GameObject instantiatedObject) => InstantiateHookCount++;

            protected override void OnRemoved(GameObject removedObject) => RemovedCount++;

            protected override void OnPoolEmpty() => PoolEmptyCount++;
        }

        /// <summary>
        /// Records the callback order and lets a test run its own code from inside a callback, which is
        /// where most of this pool's interesting failures live.
        /// </summary>
        protected sealed class RecordingListener : MonoBehaviour, IPooledObjectListener
        {
            public readonly List<string> Calls = new();

            public IPooledObjectHandle Handle { get; private set; }

            public Action<IPooledObjectHandle> WhenAcquired;
            public Action<IPooledObjectHandle> WhenReturning;

            public void OnAcquired(IPooledObjectHandle handle)
            {
                Handle = handle;
                Calls.Add(nameof(OnAcquired));
                WhenAcquired?.Invoke(handle);
            }

            public void OnReturningToPool()
            {
                Calls.Add(nameof(OnReturningToPool));
                WhenReturning?.Invoke(Handle);
            }

            public void OnReturnedToPool() => Calls.Add(nameof(OnReturnedToPool));

            public void OnDestroying() => Calls.Add(nameof(OnDestroying));
        }

        protected GameObject Template { get; private set; }

        protected TestPool Pool { get; private set; }

        [SetUp]
        public void CreatePool()
        {
            Template = new GameObject("PooledTemplate");
            Template.AddComponent<RecordingListener>();

            // Deliberately inactive, the way a prefab that disables itself on wake arrives. Get owes the
            // caller something live either way.
            Template.SetActive(false);

            Pool = new TestPool(Template);
        }

        [TearDown]
        public void DisposePool()
        {
            Pool?.Clear();

            if (Template != null)
            {
                Object.DestroyImmediate(Template);
            }
        }

        protected static IPooledObjectHandle Handle(GameObject instance) =>
            instance.GetComponent<IPooledObjectHandle>();

        protected static RecordingListener Listener(GameObject instance) =>
            instance.GetComponent<RecordingListener>();
    }
}
