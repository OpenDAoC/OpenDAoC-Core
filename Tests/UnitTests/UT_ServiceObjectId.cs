using System;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_ServiceObjectId
    {
        [SetUp]
        public void SetUp()
        {
            ServiceObjectStore.Register<TestServiceObject>();
        }

        [Test]
        public void NewId_ShouldBeUnset()
        {
            TestServiceObject owner = new();
            ServiceObjectId id = owner.ServiceObjectId;

            Assert.Multiple(() =>
            {
                Assert.That(id.Owner, Is.SameAs(owner));
                Assert.That(id.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
                Assert.That(id.IsRegistered, Is.False);
                Assert.That(id.IsRunning, Is.False);
                Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
            });
        }

        [Test]
        public void Register_ShouldQueueAddition_ThenRegisterOnUpdate()
        {
            TestServiceObject owner = new();
            ServiceObjectId id = owner.ServiceObjectId;

            Assert.That(id.Register(), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(id.IsRegistered, Is.False);
                Assert.That(id.IsRunning, Is.False);
                Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Add));
            });

            // Already queued for addition.
            Assert.That(id.Register(), Is.False);

            ServiceObjectView<TestServiceObject> view = ServiceObjectStore.UpdateAndGetView<TestServiceObject>(0);

            Assert.Multiple(() =>
            {
                Assert.That(id.IsRegistered, Is.True);
                Assert.That(id.IsRunning, Is.True);
                Assert.That(id.Value, Is.EqualTo(0));
                Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void Unregister_ShouldQueueRemoval_ThenUnregisterOnUpdate()
        {
            TestServiceObject owner = new();
            ServiceObjectId id = owner.ServiceObjectId;

            id.Register();
            ServiceObjectStore.UpdateAndGetView<TestServiceObject>(0);

            Assert.That(id.Unregister(), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(id.IsRegistered, Is.True);
                Assert.That(id.IsRunning, Is.True);
                Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Remove));
            });

            // Already queued for removal.
            Assert.That(id.Unregister(), Is.False);

            ServiceObjectView<TestServiceObject> view = ServiceObjectStore.UpdateAndGetView<TestServiceObject>(0);

            Assert.Multiple(() =>
            {
                Assert.That(id.IsRegistered, Is.False);
                Assert.That(id.IsRunning, Is.False);
                Assert.That(id.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
                Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
                Assert.That(view.TotalValidCount, Is.Zero);
            });
        }

        [Test]
        public void Register_AlreadyRegistered_ShouldReturnFalse()
        {
            TestServiceObject owner = new();
            owner.Register();
            ServiceObjectStore.UpdateAndGetView<TestServiceObject>(0);

            Assert.That(owner.Register(), Is.False);
            Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
        }

        [Test]
        public void Unregister_NotRegistered_ShouldReturnFalse()
        {
            TestServiceObject owner = new();
            Assert.That(owner.Unregister(), Is.False);
        }

        [Test]
        public void Register_WithoutPublishedArray_ShouldThrow()
        {
            TestUnregisteredServiceObject owner = new();
            Assert.Throws<InvalidOperationException>(() => owner.Register());
        }

        [Test]
        public void NextTickMs_OnNonSchedulableId_ShouldStoreValue()
        {
            TestServiceObject owner = new();
            owner.ServiceObjectId.NextTickMs = 1234;
            Assert.That(owner.ServiceObjectId.NextTickMs, Is.EqualTo(1234));
        }

        [Test]
        public void TrySetAction_ShouldOnlyReturnTrueWhenActionChanges()
        {
            TestServiceObject owner = new();
            ServiceObjectId id = owner.ServiceObjectId;

            Assert.That(id.TrySetAction(ServiceObjectId.PendingAction.Add), Is.True);
            Assert.That(id.TrySetAction(ServiceObjectId.PendingAction.Add), Is.False);
            Assert.That(id.TrySetAction(ServiceObjectId.PendingAction.Remove), Is.True);
            Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Remove));
        }

        [Test]
        public void TryConsumeAction_ShouldOnlyConsumeExpectedAction()
        {
            TestServiceObject owner = new();
            ServiceObjectId id = owner.ServiceObjectId;

            id.TrySetAction(ServiceObjectId.PendingAction.Move);

            Assert.That(id.TryConsumeAction(ServiceObjectId.PendingAction.Add), Is.False);
            Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.Move));

            Assert.That(id.TryConsumeAction(ServiceObjectId.PendingAction.Move), Is.True);
            Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));

            Assert.That(id.TryConsumeAction(ServiceObjectId.PendingAction.Move), Is.False);
        }

        [Test]
        public void Unset_ShouldClearValueAndPendingAction()
        {
            TestServiceObject owner = new();
            ServiceObjectId id = owner.ServiceObjectId;

            id.Value = 42;
            id.TrySetAction(ServiceObjectId.PendingAction.Remove);
            id.Unset();

            Assert.Multiple(() =>
            {
                Assert.That(id.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
                Assert.That(id.IsRegistered, Is.False);
                Assert.That(id.IsRunning, Is.False);
                Assert.That(id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
            });
        }

        [Test]
        public void CreateId_ShouldReturnIdTypeMatchingInterface()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ServiceObjectId.Create(new TestServiceObject()), Is.TypeOf<ServiceObjectId<TestServiceObject>>());
                Assert.That(ServiceObjectId.Create(new TestSchedulableServiceObject()), Is.TypeOf<SchedulableServiceObjectId<TestSchedulableServiceObject>>());
                Assert.That(ServiceObjectId.Create(new TestShardedServiceObject()), Is.TypeOf<ShardedServiceObjectId<TestShardedServiceObject>>());
            });
        }

        [Test]
        public void Register_ShouldCreateArrayTypeMatchingInterface()
        {
            ServiceObjectStore.Register<TestServiceObject>(capacity: 4);
            ServiceObjectStore.Register<TestSchedulableServiceObject>(capacity: 4);
            ServiceObjectStore.Register<TestShardedServiceObject>(capacity: 4);

            Assert.Multiple(() =>
            {
                Assert.That(
                    ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>(),
                    Is.TypeOf<ServiceObjectArray<TestServiceObject>>());

                Assert.That(
                    ServiceObjectStore.GetArray<TestSchedulableServiceObject, SchedulableServiceObjectId<TestSchedulableServiceObject>>(),
                    Is.TypeOf<SchedulableServiceObjectArray<TestSchedulableServiceObject>>());

                Assert.That(
                    ServiceObjectStore.GetArray<TestShardedServiceObject, ShardedServiceObjectId<TestShardedServiceObject>>(),
                    Is.TypeOf<ShardedServiceObjectArray<TestShardedServiceObject>>());
            });
        }
    }
}
