using System;
using System.Linq;
using DOL.AI;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_ServiceObjectStore
    {
        private ServiceObjectArrayBase<TestServiceObject, ServiceObjectId<TestServiceObject>> _array;

        [SetUp]
        public void SetUp()
        {
            ServiceObjectStore.Register<TestServiceObject>();
            _array = ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>();
        }

        [Test]
        public void GetArray_WhenPublished_ShouldReturnThePublishedArray()
        {
            ServiceObjectArrayBase<TestServiceObject, ServiceObjectId<TestServiceObject>> array =
                ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>();

            Assert.That(array, Is.SameAs(_array));
        }

        [Test]
        public void GetArray_WhenNotPublished_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ServiceObjectStore.GetArray<TestUnregisteredServiceObject, ServiceObjectId<TestUnregisteredServiceObject>>());
        }

        [Test]
        public void Init_ShouldDiscoverAndRegisterProductionServiceObjects()
        {
            Assert.That(ServiceObjectStore.Init(), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(
                    ServiceObjectStore.GetArray<GameClient, ServiceObjectId<GameClient>>(),
                    Is.TypeOf<ServiceObjectArray<GameClient>>());

                Assert.That(
                    ServiceObjectStore.GetArray<ABrain, ShardedServiceObjectId<ABrain>>(),
                    Is.TypeOf<ShardedServiceObjectArray<ABrain>>());

                Assert.That(
                    ServiceObjectStore.GetArray<ECSGameTimer, ShardedServiceObjectId<ECSGameTimer>>(),
                    Is.TypeOf<ShardedServiceObjectArray<ECSGameTimer>>());
            });
        }

        [Test]
        public void UpdateAndGetView_WhenNotPublished_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(() => ServiceObjectStore.UpdateAndGetView<TestUnregisteredServiceObject>(0));
        }

        [Test]
        public void UpdateAndGetView_ShouldApplyPendingAdditionsAndRemovals()
        {
            TestServiceObject a = new();
            TestServiceObject b = new();

            a.Register();
            b.Register();

            ServiceObjectView<TestServiceObject> view = ServiceObjectStore.UpdateAndGetView<TestServiceObject>(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(2));
                Assert.That(view.Items.Take(2), Is.EquivalentTo([a, b]));
            });

            a.Unregister();
            view = ServiceObjectStore.UpdateAndGetView<TestServiceObject>(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(view.Items[0], Is.SameAs(b));
                Assert.That(a.ServiceObjectId.IsRegistered, Is.False);
            });
        }

        [Test]
        public void Publish_ShouldReplaceThePreviouslyPublishedArray()
        {
            TestServiceObject owner = new();
            ServiceObjectArray<TestServiceObject> replacement = new(4);

            replacement.Publish();

            Assert.That(
                ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>(),
                Is.SameAs(replacement));

            owner.Register();
            ServiceObjectView<TestServiceObject> view = ServiceObjectStore.UpdateAndGetView<TestServiceObject>(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.Items, Is.SameAs(replacement.Items));
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(_array.TotalValidCount, Is.Zero);
            });
        }

        [Test]
        public void Register_ShouldPublishToBothViewAndOwnerSpecificSlots()
        {
            ServiceObjectStore.Register<TestSchedulableServiceObject>();
            ServiceObjectStore.Register<TestShardedServiceObject>(capacity: 16);

            Assert.Multiple(() =>
            {
                Assert.That(
                    ServiceObjectStore.GetArray<TestSchedulableServiceObject, SchedulableServiceObjectId<TestSchedulableServiceObject>>(),
                    Is.TypeOf<SchedulableServiceObjectArray<TestSchedulableServiceObject>>());

                Assert.That(
                    ServiceObjectStore.GetArray<TestShardedServiceObject, ShardedServiceObjectId<TestShardedServiceObject>>(),
                    Is.TypeOf<ShardedServiceObjectArray<TestShardedServiceObject>>());
            });
        }

        [Test]
        public void GetArray_WithNonOwningIdType_ShouldThrow()
        {
            // SchedulableServiceObject is published as a SchedulableServiceObjectId owner, not a plain ServiceObjectId owner.
            ServiceObjectStore.Register<TestSchedulableServiceObject>();

            Assert.Throws<InvalidOperationException>(() =>
                ServiceObjectStore.GetArray<TestSchedulableServiceObject, ServiceObjectId<TestSchedulableServiceObject>>());
        }

        [Test]
        public void Publish_ThroughArrayBase_ShouldBeIdempotent()
        {
            _array.Publish();
            _array.Publish();

            Assert.That(
                ServiceObjectStore.GetArray<TestServiceObject, ServiceObjectId<TestServiceObject>>(),
                Is.SameAs(_array));
        }
    }
}
