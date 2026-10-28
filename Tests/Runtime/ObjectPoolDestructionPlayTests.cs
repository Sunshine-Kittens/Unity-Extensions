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
        // Serializable so a test can write the pool's serialized fields the way deserialization does,
        // which is the only route to the inspector-write path that never runs a setter.
        [Serializable]
        private sealed class TestPool : ObjectPool
        {
            private readonly GameObject _template;

            public int PoolEmptyCount { get; private set; }

            /// <summary>The parking root, so a test can see which scene it ended up in.</summary>
            public Transform Root => PoolRoot;

            public TestPool(GameObject template) => _template = template;

            public GameObject Acquire() => Get(_template);

            protected override void OnPoolEmpty() => PoolEmptyCount++;
        }

        /// <summary>
        /// Disowns from OnDisable - the one callback the pool runs after its last ownership check, and the
        /// reason there has to be another one after it.
        /// </summary>
        private sealed class DisownOnDisable : MonoBehaviour
        {
            public IObjectPool Pool;

            private void OnDisable() => Pool?.RemoveFromPool(gameObject);
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
        public IEnumerator TheEmptyNotificationLandsAfterTheDestroyEvents()
        {
            // OnDestroying is documented as the last chance to release what an object owns. An addressable
            // pool frees its asset from OnPoolEmpty, so if the empty notification lands first, every
            // listener runs against a bundle that has already been handed back.
            GameObject instance = _pool.Acquire();

            int emptyCountWhenAnnounced = -1;
            Handle(instance).Destroyed += _ => emptyCountWhenAnnounced = _pool.PoolEmptyCount;

            Object.Destroy(instance);
            yield return null;

            Assert.That(emptyCountWhenAnnounced, Is.EqualTo(0),
                "the pool called itself empty before the destroy events ran");
            Assert.That(_pool.PoolEmptyCount, Is.EqualTo(1), "the pool never reported itself empty at all");
        }

        [UnityTest]
        public IEnumerator DisowningWhileTheObjectIsDeactivating_NeverParksIt()
        {
            GameObject instance = _pool.Acquire();
            instance.AddComponent<DisownOnDisable>().Pool = _pool;

            bool parked = _pool.ReturnToPool(instance);

            Assert.That(parked, Is.False, "the pool filed an object a callback had already taken off its books");
            Assert.That(_pool.InactiveCount, Is.EqualTo(0),
                "the parked set holds an instance with no tracking entry, and the next acquisition pops it");

            Object.Destroy(instance);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyingTheLastDetachedInstance_FinallyReportsThePoolEmpty()
        {
            // The other half of the invariant. Withholding the notification while a detached instance is
            // alive is only right if it is delivered once that instance dies - and the pool has no route
            // to that death except the handle's own event, since detaching drops its pool reference and
            // Prune walks two lists a detached instance is in neither of. Suppress-and-drop and
            // suppress-and-deliver are indistinguishable without this assertion.
            GameObject instance = _pool.Acquire();
            _pool.RemoveFromPool(instance);

            Assert.That(_pool.PoolEmptyCount, Is.EqualTo(0),
                "the pool called itself empty while the instance it had just given up was still alive");

            Object.Destroy(instance);
            yield return null;

            Assert.That(_pool.PoolEmptyCount, Is.EqualTo(1),
                "the notification was withheld and then never delivered - an asset-backed pool would " +
                "hold its asset for the life of the process");
        }

        [UnityTest]
        public IEnumerator PersistPoolRoot_KeepsTheParkedSetOutOfTheScene()
        {
            _pool.PersistPoolRoot = true;
            _pool.ReturnToPool(_pool.Acquire());

            Assert.That(_pool.Root.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"),
                "the parking root stayed in the scene, so a scene load takes the whole parked set with it");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearingPersistPoolRoot_BringsTheRootBackIntoTheScene()
        {
            _pool.PersistPoolRoot = true;
            _pool.ReturnToPool(_pool.Acquire());

            _pool.PersistPoolRoot = false;

            Assert.That(_pool.Root.gameObject.scene.name, Is.Not.EqualTo("DontDestroyOnLoad"),
                "the root stayed out of the scene, so nothing can repair a pool whose flag and root disagree");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnInspectorWrittenPersistFlag_TakesEffectOnTheNextReturn()
        {
            _pool.ReturnToPool(_pool.Acquire());
            Assert.That(_pool.Root.gameObject.scene.name, Is.Not.EqualTo("DontDestroyOnLoad"));

            // The flag is serialized, so ticking it in the inspector never runs the setter. Written here
            // the same way deserialization writes it, which is the case the getter's bool compare - rather
            // than an equality guard on the setter - exists for.
            JsonUtility.FromJsonOverwrite("{\"_persistPoolRoot\":true}", _pool);
            _pool.ReturnToPool(_pool.Acquire());

            Assert.That(_pool.Root.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"),
                "a flag written by deserialization never reached the root, so the parked set still dies with the scene");
            yield return null;
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
