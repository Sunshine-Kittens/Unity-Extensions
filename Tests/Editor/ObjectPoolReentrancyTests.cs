using System.Collections.Generic;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEngine.TestTools;

namespace UnityEngine.Extension.Tests
{
    /// <summary>
    /// The pool runs listener and subscriber code in the middle of its own bookkeeping, and every one of
    /// those callbacks can turn round and call back into the pool. Two review passes found most of their
    /// defects here, so these are the cases worth pinning down.
    /// </summary>
    public sealed class ObjectPoolReentrancyTests : ObjectPoolTestFixture
    {
        [Test]
        public void DestroyingFromAReturnCallback_NeverParksTheDoomedObject()
        {
            GameObject instance = Pool.Acquire();
            Listener(instance).WhenReturning = handle => handle.Destroy();

            bool parked = Pool.ReturnToPool(instance);

            Assert.That(parked, Is.False, "the object was destroyed mid-return, so it never reached the parked set");
            Assert.That(Pool.InactiveCount, Is.EqualTo(0), "a destroyed object was filed as though it were reusable");
            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void DestroyingFromAReturnCallback_LeavesTheStateTerminal()
        {
            GameObject instance = Pool.Acquire();
            IPooledObjectHandle handle = Handle(instance);
            Listener(instance).WhenReturning = h => h.Destroy();

            Pool.ReturnToPool(instance);

            Assert.That(handle.State, Is.EqualTo(PooledObjectState.Destroyed),
                "the return finished on top of the destruction and reset the terminal state");
        }

        [Test]
        public void DisowningFromAReturnCallback_NeverParksTheDisownedObject()
        {
            GameObject instance = Pool.Acquire();
            Listener(instance).WhenReturning = _ => Pool.RemoveFromPool(instance);

            bool parked = Pool.ReturnToPool(instance);

            Assert.That(parked, Is.False);
            Assert.That(Pool.InactiveCount, Is.EqualTo(0), "the pool parked an object it no longer owned");

            Object.DestroyImmediate(instance);
        }

        [Test]
        public void ReturningFromTheInstantiateCallback_HandsBackNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("sent the object back to the pool"));

            GameObject handedOver = Pool.Acquire(instance => Handle(instance).ReturnToPool());

            Assert.That(handedOver, Is.Null,
                "the caller and the pool would both have believed they owned it");
            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void AcquiringFromADestroySubscriber_NeverHandsBackTheDyingObject()
        {
            GameObject instance = Pool.Acquire();

            GameObject handedOut = null;
            int reentries = 0;
            Handle(instance).Destroyed += _ =>
            {
                if (reentries++ == 0)
                {
                    handedOut = Pool.Acquire();
                }
            };

            Pool.DestroyFromPool(instance);

            Assert.That(handedOut, Is.Not.Null, "the subscriber never ran");
            Assert.That(ReferenceEquals(handedOut, instance), Is.False,
                "the pool handed a subscriber the instance it was in the middle of destroying");
        }

        [Test]
        public void AcquiringWhileClearing_DoesNotLeaveTheNewObjectOrphaned()
        {
            GameObject instance = Pool.Acquire();

            GameObject rebuilt = null;
            int reentries = 0;
            Handle(instance).Destroyed += _ =>
            {
                if (reentries++ == 0)
                {
                    rebuilt = Pool.Acquire();
                }
            };

            Pool.Clear();

            Assert.That(reentries, Is.EqualTo(1), "the subscriber never ran");
            Assert.That(rebuilt == null, Is.True,
                "an object acquired during Clear survived it, owned by nothing");
            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
            Assert.That(Pool.InactiveCount, Is.EqualTo(0));
        }

        [Test]
        public void AListenerThatThrows_DoesNotLeaveTheObjectHalfReturned()
        {
            LogAssert.Expect(LogType.Exception, new Regex("deliberate"));

            GameObject instance = Pool.Acquire();
            Listener(instance).WhenReturning = _ => throw new System.InvalidOperationException("deliberate");

            Assert.That(Pool.ReturnToPool(instance), Is.True);
            Assert.That(Pool.InactiveCount, Is.EqualTo(1));
            Assert.That(Handle(instance).State, Is.EqualTo(PooledObjectState.Pooled));
        }

        [Test]
        public void ACallbackTakingOwnership_StillGetsItsObject()
        {
            // Disowning is what RemoveFromPool is for: "build me one, I will own it". That is a caller
            // taking the object, not a callback throwing it away, so Get owes it a return value.
            GameObject handedOver = Pool.Acquire(instance => Pool.RemoveFromPool(instance));

            Assert.That(handedOver, Is.Not.Null, "a legitimate hand-off was treated as a fault and discarded");
            Assert.That(handedOver.activeSelf, Is.True);
            Assert.That(Handle(handedOver).State, Is.EqualTo(PooledObjectState.Detached));
            Assert.That(Pool.Contains(handedOver), Is.False);

            Object.DestroyImmediate(handedOver);
        }

        [Test]
        public void DisowningFromAnAcquireListener_DoesNotThrowOutOfGet()
        {
            ComponentObjectPool<RecordingListener> componentPool =
                new ComponentObjectPool<RecordingListener>(Listener(Template), 0);

            GameObject built = null;
            RecordingListener.WhenAnyAcquired = listener =>
            {
                built = listener.gameObject;
                componentPool.RemoveFromPool(built);
            };

            // The listener prunes the component map before Get reads it. Both reads have to survive that.
            Assert.DoesNotThrow(() => componentPool.Get(_ => { }));

            if (built != null)
            {
                Object.DestroyImmediate(built);
            }
        }

        [Test]
        public void AClearThatCannotDrain_DetachesTheSurvivorsAndStaysQuiet()
        {
            LogAssert.Expect(LogType.Error, new Regex("could not drain"));

            System.Collections.Generic.List<GameObject> survivors = new();

            Pool.Acquire();
            RecordingListener.WhenAnyDestroying = _ => survivors.Add(Pool.Acquire());

            Pool.Clear();

            GameObject survivor = survivors[survivors.Count - 1];

            Assert.That(survivor == null, Is.False, "the last object acquired during the drain should still be alive");
            Assert.That(Handle(survivor).Pool, Is.Null,
                "a survivor left pointing at the cleared pool has no way to destroy itself through its handle");
            Assert.That(Pool.PoolEmptyCount, Is.EqualTo(0),
                "the map was emptied, not the world - announcing that frees an asset the survivors still need");

            RecordingListener.WhenAnyDestroying = null;
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i] != null)
                {
                    Object.DestroyImmediate(survivors[i]);
                }
            }
        }

        [Test]
        public void ACallbackTakingOwnership_DoesNotLetThePoolCallItselfEmpty()
        {
            // The hand-off empties the pool's map while the object it is handing over is alive. Reading
            // that as an empty pool is an asset-backed subclass's cue to free the asset - so Get would
            // return a live instance of something already unloaded.
            GameObject handedOver = Pool.Acquire(instance => Pool.RemoveFromPool(instance));

            Assert.That(Pool.PoolEmptyCount, Is.EqualTo(0),
                "the pool called itself empty while handing over a live instance built from its asset");

            Object.DestroyImmediate(handedOver);
        }

        [Test]
        public void ASecondClearAfterAFailedDrain_StillDoesNotReportEmpty()
        {
            LogAssert.Expect(LogType.Error, new Regex("could not drain"));

            List<GameObject> survivors = new();

            Pool.Acquire();
            RecordingListener.WhenAnyDestroying = _ => survivors.Add(Pool.Acquire());

            Pool.Clear();
            RecordingListener.WhenAnyDestroying = null;

            // The first Clear withheld the notification because instances survived it. The second finds
            // every collection already emptied, takes the healthy path, and would announce what the first
            // one deliberately did not - the survivors are still alive and still need the asset.
            Pool.Clear();

            Assert.That(Pool.PoolEmptyCount, Is.EqualTo(0),
                "a second Clear announced an empty pool while the first Clear's survivors were still alive");

            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i] != null)
                {
                    Object.DestroyImmediate(survivors[i]);
                }
            }
        }

        [Test]
        public void AClearThatCannotDrain_DropsItsSurvivorsFromTheSubclassRecord()
        {
            LogAssert.Expect(LogType.Error, new Regex("could not drain"));

            ComponentObjectPool<RecordingListener> componentPool =
                new ComponentObjectPool<RecordingListener>(Listener(Template), 0);

            List<GameObject> survivors = new();

            componentPool.Get();
            RecordingListener.WhenAnyDestroying = _ =>
            {
                RecordingListener rebuilt = componentPool.Get();
                if (rebuilt != null)
                {
                    survivors.Add(rebuilt.gameObject);
                }
            };

            componentPool.Clear();
            RecordingListener.WhenAnyDestroying = null;

            // Giving up ownership runs the subclass hook. Detaching the survivors without it left every
            // one of them in the component map for the life of the process, which is the leak that hook
            // exists to prevent - and these pools are static and outlive every scene.
            Assert.That(componentPool.AllComponents, Is.Empty,
                "the survivors stayed in the component map after the pool gave them up");

            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i] != null)
                {
                    Object.DestroyImmediate(survivors[i]);
                }
            }
        }

        // The paths that begin with Unity destroying an object - OnDestroy telling the pool, the ordering
        // that keeps a destroy subscriber from being handed the dying instance, and anything that turns on
        // OnDisable - live in the PlayMode suite. Edit mode runs none of those messages for these
        // components, so they cannot start here.
    }
}
