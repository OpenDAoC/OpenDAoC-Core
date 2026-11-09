using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_SchedulableServiceObjectArray
    {
        // Mirrors SchedulableServiceObjectArray's internal wheel resolution.
        private const int WHEEL_RESOLUTION_MS = 30;

        private ServiceObjectArrayBase<TestSchedulableServiceObject, SchedulableServiceObjectId<TestSchedulableServiceObject>> _array;
        private ITickExecutor _tickExecutor;

        [SetUp]
        public void SetUp()
        {
            ServiceObjectStore.Register<TestSchedulableServiceObject>();
            _array = ServiceObjectStore.GetArray<TestSchedulableServiceObject, SchedulableServiceObjectId<TestSchedulableServiceObject>>();
            _tickExecutor = TestExecutor.Instance.TickExecutor;
        }

        [Test]
        public void Register_WithDueTick_ShouldRunImmediately()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 0
            };

            Assert.That(owner.Register(), Is.True);
            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.True);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(-1));
            });
        }

        [Test]
        public void Register_WithTickInThePast_ShouldRunImmediately()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 500
            };

            owner.Register();
            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(1000);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Register_WithFutureTick_ShouldSleepUntilDue()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };

            Assert.That(owner.Register(), Is.True);

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.True);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.False);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.SLEEPING_ID));
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(1000 / WHEEL_RESOLUTION_MS));
            });

            // Still before the target bucket.
            view = _array.UpdateAndGetView(32 * WHEEL_RESOLUTION_MS);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.SLEEPING_ID));
            });

            // Reaching the target bucket wakes the item. Scheduling uses floor division, so waking up early is expected.
            view = _array.UpdateAndGetView(33 * WHEEL_RESOLUTION_MS);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(owner));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(-1));
                Assert.That(owner.Id.SleepNext, Is.Null);
                Assert.That(owner.Id.SleepPrev, Is.Null);
            });
        }

        [Test]
        public void Register_WhenSleeping_ShouldReturnFalse()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            int bucket = owner.Id.SleepBucket;

            Assert.Multiple(() =>
            {
                Assert.That(owner.Register(), Is.False);
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(bucket));
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.SLEEPING_ID));
            });
        }

        [Test]
        public void Register_WhenRunning_ShouldReturnFalse()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 0
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(owner.Register(), Is.False);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
            });
        }

        [Test]
        public void Reschedule_RunningItemToFuture_ShouldRemoveItFromActiveListThenWakeIt()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 0
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            Assert.That(owner.ServiceObjectId.IsRunning, Is.True);

            owner.NextTickMs = 600;

            Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Move));

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.True);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.False);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.SLEEPING_ID));
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(600 / WHEEL_RESOLUTION_MS));
            });

            view = _array.UpdateAndGetView(600);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Reschedule_SleepingItemToSameBucket_ShouldReturnFalse()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            int bucket = owner.Id.SleepBucket;

            // Same bucket: 1010 / 30 == 1000 / 30 == 33.
            owner.NextTickMs = 1010;

            Assert.Multiple(() =>
            {
                Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
                Assert.That(_array.Reschedule(owner.Id), Is.False);
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(bucket));
            });
        }

        [Test]
        public void Reschedule_SleepingItemToDifferentBucket_ShouldMoveIt()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            owner.NextTickMs = 300;

            Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Move));

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(300 / WHEEL_RESOLUTION_MS));
            });

            view = _array.UpdateAndGetView(300);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Reschedule_UnregisteredItem_ShouldReturnFalse()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 100
            };

            Assert.Multiple(() =>
            {
                Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
                Assert.That(_array.Reschedule(owner.Id), Is.False);
            });
        }

        [Test]
        public void Reschedule_WhileAddPending_ShouldReturnFalse()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 600
            };
            owner.Register();

            Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Add));

            owner.NextTickMs = 300;

            Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Add));

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(300 / WHEEL_RESOLUTION_MS));
            });
        }

        [Test]
        public void Reschedule_WhileRemovePending_ShouldReturnFalse()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 0
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            owner.Unregister();
            Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Remove));

            owner.NextTickMs = 600;

            Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Remove));

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
            });
        }

        [Test]
        public void Reschedule_RunningItemStillDue_ShouldReturnFalse()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(1000);

            Assert.That(owner.ServiceObjectId.IsRunning, Is.True);

            owner.NextTickMs = 1010;

            Assert.Multiple(() =>
            {
                Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
                Assert.That(_array.Reschedule(owner.Id), Is.False);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Reschedule_SleepingItemThatBecameDue_ShouldRunOnNextUpdate()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            Assert.That(owner.ServiceObjectId.IsRunning, Is.False);

            // Now the item is due, but it still sits in a bucket that wasn't processed yet.
            owner.NextTickMs = 0;
            Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Move));

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(-1));
            });
        }

        [Test]
        public void Unregister_SleepingItem_ShouldUnlinkIt()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            owner.Unregister();

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(1000);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.False);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(-1));
                Assert.That(owner.Id.SleepNext, Is.Null);
                Assert.That(owner.Id.SleepPrev, Is.Null);
            });

            // The item must not reappear once its former wake-up time is reached.
            view = _array.UpdateAndGetView(2000);
            Assert.That(view.TotalValidCount, Is.Zero);
        }

        [Test]
        public void Unregister_DueItemBeforeWakeUp_ShouldPreventWakeUp()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            owner.Unregister();

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(1000);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.False);
            });
        }

        [Test]
        public void Remove_AndReRegister_BeforeUpdate_ShouldScheduleOnce()
        {
            TestSchedulableServiceObject owner = new()
            {
                NextTickMs = 1000
            };
            owner.Register();
            _array.UpdateAndGetView(0);

            owner.Unregister();
            Assert.That(owner.Register(), Is.True);

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.True);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.False);
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(1000 / WHEEL_RESOLUTION_MS));
            });

            view = _array.UpdateAndGetView(1000);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Update_ShouldWakeItemsFromMultipleBuckets()
        {
            TestSchedulableServiceObject a = new() { NextTickMs = 100 };
            TestSchedulableServiceObject b = new() { NextTickMs = 200 };
            TestSchedulableServiceObject c = new() { NextTickMs = 300 };

            a.Register();
            b.Register();
            c.Register();

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.That(view.TotalValidCount, Is.Zero);

            view = _array.UpdateAndGetView(1000);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(3));
                Assert.That(view.Items.Take(3), Is.EqualTo([a, b, c]));
                Assert.That(a.ServiceObjectId.IsRunning, Is.True);
                Assert.That(b.ServiceObjectId.IsRunning, Is.True);
                Assert.That(c.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Update_ShouldHandleWheelWrapAround()
        {
            long baseTick = 65535;
            long baseMs = baseTick * WHEEL_RESOLUTION_MS;

            // Initialize the wheel near the wrap-around point.
            _array.UpdateAndGetView(baseMs);

            TestSchedulableServiceObject owner = new() { NextTickMs = (baseTick + 5) * WHEEL_RESOLUTION_MS };
            owner.Register();

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(baseMs);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.False);
            });

            view = _array.UpdateAndGetView((baseTick + 5) * WHEEL_RESOLUTION_MS);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Update_WithNoPendingWork_ShouldKeepItemsRunning()
        {
            TestSchedulableServiceObject owner = new() { NextTickMs = 0 };
            owner.Register();

            _array.UpdateAndGetView(0);
            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(1000);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(owner));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Add_MultipleFutureTicks_ShouldKeepAllSleepingUntilWoken()
        {
            TestSchedulableServiceObject a = new() { NextTickMs = 1000 };
            TestSchedulableServiceObject b = new() { NextTickMs = 2000 };
            TestSchedulableServiceObject c = new() { NextTickMs = 3000 };

            a.Register();
            b.Register();
            c.Register();

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);
            Assert.That(view.TotalValidCount, Is.Zero);

            view = _array.UpdateAndGetView(1000);
            Assert.That(view.TotalValidCount, Is.EqualTo(1));
            Assert.That(view.Items[0], Is.SameAs(a));

            view = _array.UpdateAndGetView(2000);
            Assert.That(view.TotalValidCount, Is.EqualTo(2));

            view = _array.UpdateAndGetView(3000);
            Assert.That(view.TotalValidCount, Is.EqualTo(3));
            Assert.That(new[] { a, b, c }.All(o => o.ServiceObjectId.IsRunning), Is.True);
        }

        [Test]
        public void Reschedule_SameObjectConcurrently_ShouldNotDuplicateIt()
        {
            TestSchedulableServiceObject owner = new() { NextTickMs = 0 };

            owner.Register();
            _array.UpdateAndGetView(0);

            Assert.That(owner.ServiceObjectId.IsRunning, Is.True);

            Parallel.For(0, 32, _ => owner.NextTickMs = 600);
            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.True);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.False);
                Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(600 / WHEEL_RESOLUTION_MS));
            });

            view = _array.UpdateAndGetView(600);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(owner));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void Register_ManyObjectsConcurrently_ShouldHonorTheirTicks()
        {
            ServiceObjectStore.Register<TestSchedulableServiceObject>(capacity: 16);
            _array = ServiceObjectStore.GetArray<TestSchedulableServiceObject, SchedulableServiceObjectId<TestSchedulableServiceObject>>();

            const int count = 1000;
            TestSchedulableServiceObject[] owners = CreateOwners(count);

            for (int i = 0; i < count; i++)
                owners[i].NextTickMs = i % 2 == 0 ? 0 : 600;

            Parallel.For(0, count, i => owners[i].Register());

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(count / 2));
                Assert.That(view.Items.Take(view.TotalValidCount).All(o => o.ServiceObjectId.IsRunning), Is.True);
                Assert.That(owners.Where(o => o.NextTickMs > 0).All(o => !o.ServiceObjectId.IsRunning), Is.True);
            });

            view = _array.UpdateAndGetView(600);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(count));
                Assert.That(view.Items.Take(count), Is.EquivalentTo(owners));
            });
        }

        [Test]
        public void ExecuteForEach_ShouldVisitOnlyActiveItems()
        {
            TestSchedulableServiceObject active = new() { NextTickMs = 0 };
            TestSchedulableServiceObject sleeping = new() { NextTickMs = 1000 };

            active.Register();
            sleeping.Register();

            ServiceObjectView<TestSchedulableServiceObject> view = _array.UpdateAndGetView(0);
            List<TestSchedulableServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.That(visited, Is.EquivalentTo([active]));
        }

        private static TestSchedulableServiceObject[] CreateOwners(int count)
        {
            TestSchedulableServiceObject[] owners = new TestSchedulableServiceObject[count];

            for (int i = 0; i < count; i++)
                owners[i] = new();

            return owners;
        }
    }
}
