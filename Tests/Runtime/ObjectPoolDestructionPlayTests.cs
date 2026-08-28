using System;
using System.Collections;

using NUnit.Framework;

using UnityEngine.TestTools;

namespace UnityEngine.Extension.PlayTests
{
    /// <summary>
    /// The paths that start with Unity destroying a pooled object rather than the pool doing it. They need
    /// play mode for two reasons: OnDestroy does not run for these components in edit mode, and Object
    /// .Destroy is deferred to the end of the frame - hence the yield before every assertion.
    /// <para>
    /// Self-contained rather than sharing the EditMode fixture, so neither assembly has to depend on the
    /// other for a harness that is barely thirty lines.
    /// </para>
    /// </summary>
    public sealed class ObjectPoolDestructionPlayTests
    {
        private sealed class TestPool : ObjectPool
        {
            private readonly GameObject _template;

            public int PoolEmptyCount { get; private set; }

            public TestPool(GameObject template) => _template = template;

            public GameObject Acquire() => Get(_template);

            protected override void OnPoolEmpty() => PoolEmptyCount++;
        }

        private GameObject _template;
        private TestPool _pool;

        [SetUp]
        public void CreatePool()
        {
            _template = new GameObject("PooledTemplate");
            _template.SetActive(false);
            _pool = new TestPool(_template);
        }

        [TearDown]
        public void DisposePool()
        {
            _pool?.Clear();

            if (_template != null)
            {
                Object.Destroy(_template);
            }
        }

        private static IPooledObjectHandle Handle(GameObject instance) =>
            instance.GetComponent<IPooledObjectHandle>();

        [UnityTest]
        public IEnumerator DestroyingALentOutInstance_TakesItOffThePoolsBooks()
        {
            GameObject instance = _pool.Acquire();

            Object.Destroy(instance);
            yield return null;

            Assert.That(_pool.ActiveCount, Is.EqualTo(0));
            Assert.That(_pool.InactiveCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DestroyingTheLastParkedInstance_ReportsThePoolEmpty()
        {
            GameObject instance = _pool.Acquire();
            _pool.ReturnToPool(instance);

            Object.Destroy(instance);
            yield return null;

            Assert.That(_pool.InactiveCount, Is.EqualTo(0));
            Assert.That(_pool.PoolEmptyCount, Is.EqualTo(1), "the pool never noticed it had emptied");
        }

        [UnityTest]
        public IEnumerator ADestroyedInstance_DetachesItsHandleFromThePool()
        {
            GameObject instance = _pool.Acquire();
            IPooledObjectHandle handle = Handle(instance);

            Object.Destroy(instance);
            yield return null;

            Assert.That(handle.State, Is.EqualTo(PooledObjectState.Destroyed));
            Assert.That(handle.Pool, Is.Null);
        }

        [UnityTest]
        public IEnumerator AcquiringFromASubscriberDuringAnOutrightDestroy_NeverHandsBackTheDyingObject()
        {
            // The same trap as the pool-driven destroy, reached the other way: Unity tears the object down
            // and the handle announces it. The announcement has to come after the pool has let go, or a
            // subscriber that acquires is handed the instance that is in the middle of dying.
            GameObject instance = _pool.Acquire();

            GameObject handedOut = null;
            int reentries = 0;
            Handle(instance).Destroyed += _ =>
            {
                if (reentries++ == 0)
                {
                    handedOut = _pool.Acquire();
                }
            };

            Object.Destroy(instance);
            yield return null;

            Assert.That(reentries, Is.EqualTo(1), "the subscriber never ran");
            Assert.That(handedOut, Is.Not.Null);
            Assert.That(ReferenceEquals(handedOut, instance), Is.False,
                "the pool handed a subscriber the instance Unity was in the middle of destroying");
        }

        [UnityTest]
        public IEnumerator DestroyingTheParentOfAParkedInstance_DoesNotTakeThatInstanceWithIt()
        {
            GameObject borrower = new GameObject("Borrower");
            GameObject instance = _pool.Acquire();
            instance.transform.SetParent(borrower.transform, false);

            _pool.ReturnToPool(instance);
            Object.Destroy(borrower);
            yield return null;

            Assert.That(instance == null, Is.False, "the parked instance went down with the transform that borrowed it");
            Assert.That(_pool.InactiveCount, Is.EqualTo(1));
        }
    }
}
