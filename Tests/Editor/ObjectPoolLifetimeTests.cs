using NUnit.Framework;

namespace UnityEngine.Extension.Tests
{
    /// <summary>
    /// Parking, prewarming, budgeting and teardown - and the pool's ability to cope with instances that
    /// were destroyed without it being told.
    /// </summary>
    public sealed class ObjectPoolLifetimeTests : ObjectPoolTestFixture
    {
        [Test]
        public void ParkedObjectsSurviveTheParentThatBorrowedThem()
        {
            GameObject borrower = new GameObject("Borrower");
            GameObject instance = Pool.Acquire();
            instance.transform.SetParent(borrower.transform, false);

            Pool.ReturnToPool(instance);
            Object.DestroyImmediate(borrower);

            Assert.That(instance == null, Is.False, "the parked instance went down with the transform that borrowed it");
            Assert.That(Pool.InactiveCount, Is.EqualTo(1));
        }

        [Test]
        public void AcquiringLiftsAnObjectBackOffTheParkingRoot()
        {
            GameObject instance = Pool.Acquire();
            Assert.That(instance.transform.parent, Is.Null, "a freshly built instance should arrive unparented");

            Pool.ReturnToPool(instance);
            Assert.That(instance.transform.parent, Is.Not.Null, "a returned instance should be parked under the pool root");

            GameObject reused = Pool.Acquire();
            Assert.That(reused.transform.parent, Is.Null, "a reused instance should arrive the same way a new one does");
        }

        [Test]
        public void PrewarmParksTheRequestedCount()
        {
            Pool.Fill(3);

            Assert.That(Pool.InactiveCount, Is.EqualTo(3));
            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
            Assert.That(Pool.InstantiatedCount, Is.EqualTo(3));
        }

        [Test]
        public void PrewarmCountsWhatIsParked_NotWhatIsAlreadyLentOut()
        {
            Pool.Acquire();
            Pool.Acquire();

            Pool.Fill(3);

            Assert.That(Pool.InactiveCount, Is.EqualTo(3),
                "objects already lent out cannot serve the next acquisition, so they cannot count towards being ready for it");
        }

        [Test]
        public void PrewarmedHandlesReportPooled()
        {
            Pool.Fill(1);
            GameObject parked = Pool.Acquire();

            Assert.That(Pool.InstantiatedCount, Is.EqualTo(1), "the prewarmed instance should have been reused");
            Assert.That(Handle(parked).State, Is.EqualTo(PooledObjectState.Active));
        }

        [Test]
        public void ReturningPastTheCeiling_DestroysInsteadOfParking()
        {
            Pool.MaxParked = 1;

            GameObject first = Pool.Acquire();
            GameObject second = Pool.Acquire();

            Pool.ReturnToPool(first);
            Pool.ReturnToPool(second);

            Assert.That(Pool.InactiveCount, Is.EqualTo(1));
            Assert.That(second == null, Is.True, "the object past the ceiling should have been destroyed, not kept");
        }

        [Test]
        public void TrimDestroysDownToTheKeepCount()
        {
            Pool.Fill(4);

            Assert.That(Pool.Trim(1), Is.EqualTo(3));
            Assert.That(Pool.InactiveCount, Is.EqualTo(1));
        }

        [Test]
        public void PruneRecoversFromAnInstanceDestroyedBehindThePoolsBack()
        {
            GameObject instance = Pool.Acquire();
            Pool.ReturnToPool(instance);

            Object.DestroyImmediate(instance);
            Pool.Prune();

            // Edit mode does not run OnDestroy for these components, so the pool genuinely never hears
            // about this one - which is exactly the state Prune exists to clean up.
            Assert.That(Pool.InactiveCount, Is.EqualTo(0));
            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void ADestroyedParkedInstanceDoesNotStallTheNextAcquisition()
        {
            GameObject instance = Pool.Acquire();
            Pool.ReturnToPool(instance);
            Object.DestroyImmediate(instance);

            GameObject replacement = Pool.Acquire();

            Assert.That(replacement == null, Is.False, "the pool handed back the destroyed instance");
            Assert.That(Pool.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void TrimmingADestroyedInstance_LeavesThePoolAbleToReportItselfEmpty()
        {
            GameObject instance = Pool.Acquire();
            Pool.ReturnToPool(instance);
            Object.DestroyImmediate(instance);

            Pool.Trim(0);

            Assert.That(Pool.InactiveCount, Is.EqualTo(0));
            Assert.That(Pool.PoolEmptyCount, Is.GreaterThanOrEqualTo(1),
                "a tracking entry left behind by a destroyed instance stops the pool ever reporting itself empty");
        }

        [Test]
        public void ClearReportsEmptyEvenWhenNothingWasEverBuilt()
        {
            Pool.Clear();

            Assert.That(Pool.PoolEmptyCount, Is.EqualTo(1),
                "Clear is a teardown, not a transition - a subclass with an asset to release has to hear about it");
        }

        [Test]
        public void ClearDestroysEverythingItHolds()
        {
            GameObject lentOut = Pool.Acquire();
            GameObject parked = Pool.Acquire();
            Pool.ReturnToPool(parked);

            Pool.Clear();

            Assert.That(lentOut == null, Is.True);
            Assert.That(parked == null, Is.True);
            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
            Assert.That(Pool.InactiveCount, Is.EqualTo(0));
        }

        [Test]
        public void RemoveFromPoolLiftsAParkedObjectOffTheRootItWouldBeDestroyedWith()
        {
            GameObject instance = Pool.Acquire();
            Pool.ReturnToPool(instance);

            Assert.That(Pool.RemoveFromPool(instance), Is.True);
            Assert.That(instance.transform.parent, Is.Null, "a disowned object left under the pool root dies with it");

            Pool.Clear();

            Assert.That(instance == null, Is.False, "Clear destroyed an object the pool had already given up");
            Object.DestroyImmediate(instance);
        }

        [Test]
        public void RemoveFromPoolDetachesTheHandle()
        {
            GameObject instance = Pool.Acquire();
            Pool.RemoveFromPool(instance);

            Assert.That(Handle(instance).Pool, Is.Null);
            Assert.That(Handle(instance).State, Is.EqualTo(PooledObjectState.Detached));
            Assert.That(Pool.Contains(instance), Is.False);

            Object.DestroyImmediate(instance);
        }

        [Test]
        public void ContainsTracksOwnershipThroughTheWholeCycle()
        {
            GameObject instance = Pool.Acquire();
            Assert.That(Pool.Contains(instance), Is.True);

            Pool.ReturnToPool(instance);
            Assert.That(Pool.Contains(instance), Is.True, "a parked object is still the pool's");

            Pool.DestroyFromPool(instance);
            Assert.That(Pool.Contains(instance), Is.False);
        }

        [Test]
        public void SubclassHooksFireOncePerInstanceEachWay()
        {
            GameObject instance = Pool.Acquire();
            Assert.That(Pool.InstantiateHookCount, Is.EqualTo(1));
            Assert.That(Pool.RemovedCount, Is.EqualTo(0));

            Pool.DestroyFromPool(instance);
            Assert.That(Pool.RemovedCount, Is.EqualTo(1));
        }
    }
}
