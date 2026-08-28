using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NUnit.Framework;

using UnityEngine.TestTools;

namespace UnityEngine.Extension.PlayTests
{
    /// <summary>
    /// The load gate: one load however many callers arrive, releasing a spent attempt before starting
    /// another, and discarding a load that resolved after the asset went back.
    /// <para>
    /// Driven through a fake asset source rather than Addressables. That is not a shortcut - it is the
    /// only way to hold a load open across an arbitrary number of frames and release it at an exact
    /// moment, which is what every one of these cases turns on. What it does not cover is the three lines
    /// in each concrete pool that actually call Addressables.
    /// </para>
    /// </summary>
    public sealed class AssetBackedObjectPoolPlayTests
    {
        private sealed class FakeAssetPool : AssetBackedObjectPool
        {
            private readonly GameObject _prefab;

            private GameObject _loaded;
            private TaskCompletionSource<GameObject> _pending;

            // Mirrors AsyncOperationHandle.IsValid: a handle exists from the moment a load starts and
            // survives a load that resolves to nothing. That surviving handle is the whole of F3 - without
            // it here, the fake would let the defect pass.
            private bool _holdsHandle;

            public int LoadsStarted { get; private set; }
            public int Releases { get; private set; }
            public int ReleasesWhenLastLoadStarted { get; private set; }
            public int PoolEmptyCount { get; private set; }

            public readonly List<GameObject> Results = new();
            public int Completed { get; private set; }

            public FakeAssetPool(GameObject prefab) => _prefab = prefab;

            protected override GameObject Template => _loaded;

            protected override Task<GameObject> BeginLoad()
            {
                LoadsStarted++;
                ReleasesWhenLastLoadStarted = Releases;
                _holdsHandle = true;
                _pending = new TaskCompletionSource<GameObject>();
                return _pending.Task;
            }

            protected override void ReleaseLoaded()
            {
                if (!_holdsHandle)
                {
                    return;
                }

                Releases++;
                _holdsHandle = false;
                _loaded = null;

                // Mirrors Addressables: releasing an operation that is still in flight completes it rather
                // than leaving whoever is awaiting it hanging.
                TaskCompletionSource<GameObject> pending = _pending;
                _pending = null;
                pending?.TrySetResult(null);
            }

            protected override void OnPoolEmpty()
            {
                PoolEmptyCount++;
                base.OnPoolEmpty();
            }

            public void FinishLoad()
            {
                _loaded = _prefab;
                _pending?.TrySetResult(_prefab);
                _pending = null;
            }

            public void FailLoad()
            {
                // Resolves to nothing but keeps the handle, exactly as a failed Addressables load does.
                _loaded = null;
                TaskCompletionSource<GameObject> pending = _pending;
                _pending = null;
                pending?.TrySetResult(null);
            }

            public async void BeginAcquire()
            {
                GameObject template = await GetTemplate();
                Results.Add(template == null ? null : Get(template));
                Completed++;
            }

            /// <summary>Acquires without going through the gate, for tests that need it inside a callback.</summary>
            public GameObject AcquireLoaded() => Template == null ? null : Get(Template);
        }

        private GameObject _prefab;
        private FakeAssetPool _pool;

        // Gates the drain-failure cascade. Left running, it rebuilds the pool as the test cleans up and
        // teardown's own Clear fails a second time, logging an error no expectation is waiting for.
        private bool _reacquireOnDestroy;

        [SetUp]
        public void CreatePool()
        {
            _prefab = new GameObject("AssetTemplate");
            _prefab.SetActive(false);
            _pool = new FakeAssetPool(_prefab);
            _reacquireOnDestroy = false;
        }

        [TearDown]
        public void DisposePool()
        {
            _pool?.Clear();

            if (_prefab != null)
            {
                Object.Destroy(_prefab);
            }
        }

        private IEnumerator WaitForCompletions(int count)
        {
            for (int frame = 0; frame < 60 && _pool.Completed < count; frame++)
            {
                yield return null;
            }

            Assert.That(_pool.Completed, Is.EqualTo(count), "an acquisition never finished");
        }

        [UnityTest]
        public IEnumerator OverlappingAcquisitions_ShareASingleLoad()
        {
            _pool.BeginAcquire();
            _pool.BeginAcquire();
            yield return null;

            Assert.That(_pool.LoadsStarted, Is.EqualTo(1),
                "each caller started its own load, and every handle but the last would be overwritten unreleased");

            _pool.FinishLoad();
            yield return WaitForCompletions(2);

            Assert.That(_pool.Results[0], Is.Not.Null);
            Assert.That(_pool.Results[1], Is.Not.Null);
            Assert.That(_pool.ActiveCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator AFailedLoad_IsHandedBackBeforeTheNextAttemptStarts()
        {
            _pool.BeginAcquire();
            yield return null;

            _pool.FailLoad();
            yield return WaitForCompletions(1);

            Assert.That(_pool.Results[0], Is.Null);

            _pool.BeginAcquire();
            yield return null;

            Assert.That(_pool.LoadsStarted, Is.EqualTo(2), "the second attempt never started");
            Assert.That(_pool.ReleasesWhenLastLoadStarted, Is.EqualTo(1),
                "the spent attempt was assigned over rather than released, which is the leak the gate exists to stop");

            _pool.FinishLoad();
            yield return WaitForCompletions(2);
        }

        [UnityTest]
        public IEnumerator ReleasingDuringAnInFlightLoad_DiscardsTheResult()
        {
            _pool.BeginAcquire();
            yield return null;

            // Clear releases the asset. The load is still resolving against it.
            _pool.Clear();
            yield return WaitForCompletions(1);

            Assert.That(_pool.Results[0], Is.Null,
                "an object was built from an asset that had already been handed back");
            Assert.That(_pool.ActiveCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ALoadStartedAfterARelease_StillResolves()
        {
            _pool.BeginAcquire();
            yield return null;
            _pool.Clear();
            yield return WaitForCompletions(1);

            // The generation bump must invalidate the in-flight load without wedging the gate shut.
            _pool.BeginAcquire();
            yield return null;
            _pool.FinishLoad();
            yield return WaitForCompletions(2);

            Assert.That(_pool.Results[1], Is.Not.Null, "the pool could not load again after a release");
        }

        [UnityTest]
        public IEnumerator TheAssetIsReleasedOnlyAfterTheDestroyEventsHaveRun()
        {
            _pool.BeginAcquire();
            yield return null;
            _pool.FinishLoad();
            yield return WaitForCompletions(1);

            GameObject instance = _pool.Results[0];
            int releasesWhenAnnounced = -1;
            instance.GetComponent<IPooledObjectHandle>().Destroyed += _ => releasesWhenAnnounced = _pool.Releases;

            Object.Destroy(instance);
            yield return null;

            Assert.That(releasesWhenAnnounced, Is.EqualTo(0),
                "the asset went back before OnDestroying ran, which is documented as the last chance to use it");
            Assert.That(_pool.Releases, Is.EqualTo(1), "the asset was never released at all");
        }

        [UnityTest]
        public IEnumerator AClearThatCannotDrain_KeepsTheAsset()
        {
            LogAssert.Expect(LogType.Error, new Regex("could not drain"));

            _pool.BeginAcquire();
            yield return null;
            _pool.FinishLoad();
            yield return WaitForCompletions(1);

            List<GameObject> survivors = new();
            _reacquireOnDestroy = true;
            ReacquireOnDestroy(_pool.Results[0], survivors);

            _pool.Clear();

            Assert.That(_pool.Releases, Is.EqualTo(0),
                "the asset was handed back while instances built from it were still alive in the world");

            _reacquireOnDestroy = false;

            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i] != null)
                {
                    Object.Destroy(survivors[i]);
                }
            }
            yield return null;
        }

        /// <summary>
        /// Rebuilds on every destroy, resubscribing each time because a subscription lasts one acquisition.
        /// That is what stops the drain ever finishing.
        /// </summary>
        private void ReacquireOnDestroy(GameObject instance, List<GameObject> survivors)
        {
            instance.GetComponent<IPooledObjectHandle>().Destroyed += _ =>
            {
                if (!_reacquireOnDestroy)
                {
                    return;
                }

                GameObject rebuilt = _pool.AcquireLoaded();
                if (rebuilt == null)
                {
                    return;
                }

                survivors.Add(rebuilt);
                ReacquireOnDestroy(rebuilt, survivors);
            };
        }
    }
}
