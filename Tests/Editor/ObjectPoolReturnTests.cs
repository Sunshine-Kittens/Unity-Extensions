using NUnit.Framework;

namespace UnityEngine.Extension.Tests
{
    /// <summary>
    /// The defect this whole body of work started from: returning through the pool and returning through
    /// the handle did different things. These pin the two paths to the same flow.
    /// </summary>
    public sealed class ObjectPoolReturnTests : ObjectPoolTestFixture
    {
        private static readonly string[] ReturnFlow =
        {
            nameof(IPooledObjectListener.OnReturningToPool),
            nameof(IPooledObjectListener.OnReturnedToPool)
        };

        [Test]
        public void ReturningThroughThePool_RunsTheListenerFlow()
        {
            GameObject instance = Pool.Acquire();
            RecordingListener listener = Listener(instance);
            listener.Calls.Clear();

            Assert.That(Pool.ReturnToPool(instance), Is.True);
            Assert.That(listener.Calls, Is.EqualTo(ReturnFlow));
        }

        [Test]
        public void ReturningThroughTheHandle_RunsTheSameListenerFlow()
        {
            GameObject instance = Pool.Acquire();
            RecordingListener listener = Listener(instance);
            listener.Calls.Clear();

            Handle(instance).ReturnToPool();
            Assert.That(listener.Calls, Is.EqualTo(ReturnFlow));
        }

        [Test]
        public void ReturningEverythingAtOnce_RunsTheFlowForEach()
        {
            GameObject first = Pool.Acquire();
            GameObject second = Pool.Acquire();
            Listener(first).Calls.Clear();
            Listener(second).Calls.Clear();

            Pool.ReturnAllToPool();

            Assert.That(Listener(first).Calls, Is.EqualTo(ReturnFlow));
            Assert.That(Listener(second).Calls, Is.EqualTo(ReturnFlow));
            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
            Assert.That(Pool.InactiveCount, Is.EqualTo(2));
        }

        [Test]
        public void ReturningThroughThePool_RaisesTheHandleEvent()
        {
            GameObject instance = Pool.Acquire();
            IPooledObjectHandle handle = Handle(instance);

            IPooledObjectHandle raised = null;
            handle.ReturnedToPool += h => raised = h;

            Pool.ReturnToPool(instance);

            Assert.That(raised, Is.SameAs(handle));
        }

        [Test]
        public void AcquiringMarksTheHandleActive_AndReturningMarksItPooled()
        {
            GameObject instance = Pool.Acquire();
            Assert.That(Handle(instance).State, Is.EqualTo(PooledObjectState.Active));

            Pool.ReturnToPool(instance);
            Assert.That(Handle(instance).State, Is.EqualTo(PooledObjectState.Pooled));
        }

        [Test]
        public void GetHandsBackSomethingLive_EvenWhenTheTemplateIsNot()
        {
            Assert.That(Template.activeSelf, Is.False, "the fixture's template should start inactive");

            GameObject first = Pool.Acquire();
            Assert.That(first.activeSelf, Is.True, "the freshly built instance came back inactive");

            Pool.ReturnToPool(first);
            GameObject reused = Pool.Acquire();
            Assert.That(reused.activeSelf, Is.True, "the reused instance came back inactive");
        }

        [Test]
        public void ReturningTwice_ReportsFalseTheSecondTime()
        {
            GameObject instance = Pool.Acquire();

            Assert.That(Pool.ReturnToPool(instance), Is.True);
            Assert.That(Pool.ReturnToPool(instance), Is.False);
            Assert.That(Pool.InactiveCount, Is.EqualTo(1), "the second return filed the object twice");
        }

        [Test]
        public void ReturningSomethingThePoolDoesNotOwn_ReportsFalse()
        {
            GameObject stranger = new GameObject("Stranger");

            try
            {
                Assert.That(Pool.ReturnToPool(stranger), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(stranger);
            }
        }

        [Test]
        public void SubscriptionsDoNotSurviveTheAcquisitionTheyWereMadeIn()
        {
            GameObject instance = Pool.Acquire();

            int raised = 0;
            Handle(instance).ReturnedToPool += _ => raised++;

            Pool.ReturnToPool(instance);

            GameObject reused = Pool.Acquire();
            Assert.That(ReferenceEquals(reused, instance), Is.True, "the pool should have reused the parked instance");

            Pool.ReturnToPool(reused);

            Assert.That(raised, Is.EqualTo(1), "a subscription outlived the acquisition it was made in");
        }

        [Test]
        public void ReusingAnInstance_BuildsNothingNew()
        {
            GameObject instance = Pool.Acquire();
            Pool.ReturnToPool(instance);
            Pool.Acquire();

            Assert.That(Pool.InstantiatedCount, Is.EqualTo(1));
            Assert.That(Pool.ReusedCount, Is.EqualTo(1));
            Assert.That(Pool.PeakActiveCount, Is.EqualTo(1));
        }
    }
}
