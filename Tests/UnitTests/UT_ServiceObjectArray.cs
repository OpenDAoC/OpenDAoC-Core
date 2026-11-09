using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_ServiceObjectArray
    {
        private ServiceObjectArrayBase<TestServiceObject, ServiceObjectId<TestServiceObject>> _array;
        private ITickExecutor _tickExecutor;

        [SetUp]
        public void SetUp()
        {
            ServiceObjectStore.Register<TestServiceObject>();
            _array = ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>();
            _tickExecutor = TestExecutor.Instance.TickExecutor;
        }

        [Test]
        public void NewArray_ShouldBeEmpty()
        {
            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(_array.LastValidIndex, Is.EqualTo(-1));
                Assert.That(_array.TotalValidCount, Is.Zero);
                Assert.That(_array.Items, Is.Not.Null);
                Assert.That(_array.Shards, Is.Null);
                Assert.That(_array.ShardStartIndices, Is.Null);
                Assert.That(view.IsSharded, Is.False);
                Assert.That(view.Items, Is.SameAs(_array.Items));
                Assert.That(view.TotalValidCount, Is.Zero);
            });
        }

        [Test]
        public void Add_ShouldQueueItem_ThenAddItOnUpdate()
        {
            TestServiceObject owner = new();

            Assert.That(owner.Register(), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(_array.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Add));
            });

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(owner));
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
                Assert.That(owner.ServiceObjectId.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
            });
        }

        [Test]
        public void Add_MultipleItems_ShouldAssignContiguousIndices()
        {
            TestServiceObject[] owners = CreateOwners(3);

            foreach (TestServiceObject owner in owners)
                Assert.That(owner.Register(), Is.True);

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.That(view.TotalValidCount, Is.EqualTo(owners.Length));

            for (int i = 0; i < owners.Length; i++)
            {
                Assert.That(view.Items[i], Is.SameAs(owners[i]));
                Assert.That(owners[i].ServiceObjectId.Value, Is.EqualTo(i));
            }
        }

        [Test]
        public void Add_WhenAlreadyRegistered_ShouldReturnFalse()
        {
            TestServiceObject owner = new();
            owner.Register();
            _array.UpdateAndGetView(0);

            Assert.That(owner.Register(), Is.False);
            Assert.That(_array.UpdateAndGetView(0).TotalValidCount, Is.EqualTo(1));
        }

        [Test]
        public void Remove_WhenNotRegistered_ShouldReturnFalse()
        {
            TestServiceObject owner = new();
            Assert.That(owner.Unregister(), Is.False);
        }

        [Test]
        public void Remove_MiddleItem_ShouldSwapLastItemIntoItsSlot()
        {
            TestServiceObject a = new();
            TestServiceObject b = new();
            TestServiceObject c = new();

            a.Register();
            b.Register();
            c.Register();
            _array.UpdateAndGetView(0);

            Assert.That(b.Unregister(), Is.True);
            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(2));
                Assert.That(_array.LastValidIndex, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(a));
                Assert.That(view.Items[1], Is.SameAs(c));
                Assert.That(view.Items[2], Is.Null);
                Assert.That(a.ServiceObjectId.Value, Is.EqualTo(0));
                Assert.That(c.ServiceObjectId.Value, Is.EqualTo(1));
                Assert.That(b.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
            });
        }

        [Test]
        public void Remove_LastItem_ShouldClearItsSlot()
        {
            TestServiceObject a = new();
            TestServiceObject b = new();

            a.Register();
            b.Register();
            _array.UpdateAndGetView(0);

            b.Unregister();
            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(a));
                Assert.That(view.Items[1], Is.Null);
                Assert.That(b.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
            });
        }

        [Test]
        public void Remove_AllItems_ShouldResetLastValidIndex()
        {
            TestServiceObject a = new();
            TestServiceObject b = new();

            a.Register();
            b.Register();
            _array.UpdateAndGetView(0);

            a.Unregister();
            b.Unregister();

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(_array.LastValidIndex, Is.EqualTo(-1));
                Assert.That(view.Items[0], Is.Null);
                Assert.That(view.Items[1], Is.Null);
            });
        }

        [Test]
        public void Unregister_PendingAddition_ShouldCancelAddition()
        {
            TestServiceObject owner = new();
            owner.Register();

            Assert.That(owner.Unregister(), Is.True);
            Assert.That(owner.ServiceObjectId.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Remove));

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
                Assert.That(owner.ServiceObjectId.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
            });
        }

        [Test]
        public void Register_PendingRemoval_ShouldKeepItemRegisteredOnce()
        {
            TestServiceObject owner = new();
            owner.Register();
            _array.UpdateAndGetView(0);

            owner.Unregister();
            Assert.That(owner.Register(), Is.True);

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(owner));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
            });
        }

        [Test]
        public void Remove_AndReRegister_AfterUpdate_ShouldReuseArraySlot()
        {
            TestServiceObject owner = new();
            owner.Register();
            _array.UpdateAndGetView(0);

            owner.Unregister();
            _array.UpdateAndGetView(0);

            owner.Register();
            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(owner));
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
            });
        }

        [Test]
        public void Add_BeyondInitialCapacity_ShouldGrowArray()
        {
            ServiceObjectStore.Register<TestServiceObject>(capacity: 2);
            _array = ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>();

            const int itemCount = 100;
            TestServiceObject[] owners = CreateOwners(itemCount);

            foreach (TestServiceObject owner in owners)
                owner.Register();

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.That(view.TotalValidCount, Is.EqualTo(itemCount));
            Assert.That(view.Items.Take(itemCount), Is.EquivalentTo(owners));

            for (int i = 0; i < itemCount; i++)
                Assert.That(view.Items[i].ServiceObjectId.Value, Is.EqualTo(i));
        }

        [Test]
        public void Reschedule_ShouldReturnFalse()
        {
            TestServiceObject owner = new();
            owner.Register();
            _array.UpdateAndGetView(0);

            Assert.That(_array.Reschedule((ServiceObjectId<TestServiceObject>) owner.ServiceObjectId), Is.False);
        }

        [Test]
        public void UpdateAndGetView_ShouldExecuteForEachOnValidItems()
        {
            TestServiceObject a = new();
            TestServiceObject b = new();
            TestServiceObject c = new();

            a.Register();
            b.Register();
            c.Register();

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);
            List<TestServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.That(visited, Is.EquivalentTo([a, b, c]));
        }

        [Test]
        public void UpdateAndGetView_EmptyArray_ShouldNotExecuteForEach()
        {
            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);
            int callCount = 0;

            _tickExecutor.ExecuteView(view, _ => callCount++);

            Assert.That(callCount, Is.Zero);
        }

        [Test]
        public void ExecuteForEach_AfterRemoval_ShouldNotVisitClearedSlots()
        {
            TestServiceObject a = new();
            TestServiceObject b = new();

            a.Register();
            b.Register();
            _array.UpdateAndGetView(0);

            b.Unregister();
            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);
            List<TestServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.Multiple(() =>
            {
                Assert.That(_array.Items[1], Is.Null);
                Assert.That(visited, Is.EqualTo([a]));
            });
        }

        [Test]
        public void UpdateAndGetView_ShouldReturnSnapshotOfTheCurrentCount()
        {
            TestServiceObject a = new();
            a.Register();

            ServiceObjectView<TestServiceObject> firstView = _array.UpdateAndGetView(0);
            TestServiceObject b = new();
            b.Register();

            Assert.That(firstView.TotalValidCount, Is.EqualTo(1));

            ServiceObjectView<TestServiceObject> secondView = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(firstView.TotalValidCount, Is.EqualTo(1));
                Assert.That(secondView.TotalValidCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void UpdateAndGetView_WhenAlreadyUpdating_ShouldThrow()
        {
            ReentrantServiceObjectArray<TestServiceObject> array = new(4);
            array.Publish();

            Assert.Throws<InvalidOperationException>(() => array.UpdateAndGetView(0));

            // The guard must be released even when an update fails.
            Assert.That(array.UpdateAndGetView(0).TotalValidCount, Is.Zero);
        }

        [Test]
        public void Register_ManyDistinctObjectsConcurrently_ShouldAddThemAll()
        {
            ServiceObjectStore.Register<TestServiceObject>(capacity: 16);
            _array = ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>();

            const int count = 2000;
            TestServiceObject[] owners = CreateOwners(count);

            Parallel.For(0, count, i => owners[i].Register());

            ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(count));
                Assert.That(view.Items.Take(count), Is.EquivalentTo(owners));
                Assert.That(new HashSet<int>(owners.Select(o => o.ServiceObjectId.Value)).Count, Is.EqualTo(count));
            });
        }

        [Test]
        public void Register_SameObjectConcurrently_ShouldAddItExactlyOnce()
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                ServiceObjectStore.Register<TestServiceObject>(capacity: 4);
                _array = ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>();
                TestServiceObject owner = new();

                Parallel.For(0, 32, _ => owner.Register());

                ServiceObjectView<TestServiceObject> view = _array.UpdateAndGetView(0);

                Assert.Multiple(() =>
                {
                    Assert.That(view.TotalValidCount, Is.EqualTo(1), $"iteration {iteration}");
                    Assert.That(view.Items[0], Is.SameAs(owner));
                    Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                });
            }
        }

        private static TestServiceObject[] CreateOwners(int count)
        {
            TestServiceObject[] owners = new TestServiceObject[count];

            for (int i = 0; i < count; i++)
                owners[i] = new();

            return owners;
        }

        private sealed class ReentrantServiceObjectArray<T> : ServiceObjectArray<T, ServiceObjectId<T>>
            where T : class, IServiceObject<T>
        {
            private bool _reentered;

            public ReentrantServiceObjectArray(int capacity) : base(capacity) { }

            protected override void ProcessItemsToAdd()
            {
                if (_reentered)
                {
                    base.ProcessItemsToAdd();
                    return;
                }

                _reentered = true;
                UpdateAndGetView(0);
                base.ProcessItemsToAdd();
            }
        }
    }
}
