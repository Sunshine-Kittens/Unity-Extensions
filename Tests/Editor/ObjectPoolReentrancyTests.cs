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

        // The paths that begin with Unity destroying an object - OnDestroy telling the pool, and the
        // ordering that keeps a destroy subscriber from being handed the dying instance - live in the
        // PlayMode suite. Edit mode does not run OnDestroy for these components, so they cannot start here.
    }
}
