using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_ShardedServiceObjectArray
    {
        private ServiceObjectArrayBase<TestShardedServiceObject, ShardedServiceObjectId<TestShardedServiceObject>> _array;
        private int _shardCount;
        private ITickExecutor _tickExecutor;

        [SetUp]
        public void SetUp()
        {
            ServiceObjectStore.Register<TestShardedServiceObject>(capacity: 16);
            _array = ServiceObjectStore.GetArray<TestShardedServiceObject, ShardedServiceObjectId<TestShardedServiceObject>>();
            _shardCount = _array.Shards.Length;
            _tickExecutor = TestExecutor.Instance.TickExecutor;
        }

        [Test]
        public void NewArray_ShouldExposeShardedView()
        {
            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(_array.Items, Is.Null);
                Assert.That(_array.Shards, Has.Length.EqualTo(_shardCount));
                Assert.That(_array.ShardStartIndices, Has.Length.EqualTo(_shardCount));
                Assert.That(_array.TotalValidCount, Is.Zero);
                Assert.That(_array.LastValidIndex, Is.EqualTo(-1));
                Assert.That(view.IsSharded, Is.True);
                Assert.That(view.Items, Is.Null);
                Assert.That(view.Shards, Is.SameAs(_array.Shards));
                Assert.That(view.ShardStartIndices, Is.SameAs(_array.ShardStartIndices));
                Assert.That(view.TotalValidCount, Is.Zero);
            });
        }

        [Test]
        public void Register_ShouldAssignAShardAndAddItemToIt()
        {
            TestShardedServiceObject owner = new();

            Assert.That(owner.Register(), Is.True);
            Assert.That(owner.Id.ShardIndex, Is.InRange(0, _shardCount - 1));

            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                Assert.That(owner.ServiceObjectId.Value, Is.EqualTo(0));
                Assert.That(_array.Shards[owner.Id.ShardIndex][0], Is.SameAs(owner));
                Assert.That(_array.ShardStartIndices[owner.Id.ShardIndex], Is.Zero);
            });
        }

        [Test]
        public void Register_ManyItems_ShouldDistributeThemRoundRobin()
        {
            List<TestShardedServiceObject> owners = CreateOwners(_shardCount * 2);

            foreach (TestShardedServiceObject owner in owners)
                owner.Register();

            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(owners.Count));
                Assert.That(owners.Select(o => o.Id.ShardIndex).Distinct().Count(), Is.EqualTo(_shardCount));
            });

            for (int i = 0; i < _shardCount; i++)
            {
                Assert.That(_array.Shards[i], Has.Count.EqualTo(2));
                Assert.That(_array.ShardStartIndices[i], Is.EqualTo(i * 2));
            }
        }

        [Test]
        public void Remove_ItemFromShard_ShouldCompactThatShard()
        {
            List<TestShardedServiceObject> owners = CreateOwners(_shardCount * 2);

            foreach (TestShardedServiceObject owner in owners)
                owner.Register();

            _array.UpdateAndGetView(0);

            TestShardedServiceObject removed = owners[0];
            int shardIndex = removed.Id.ShardIndex;
            TestShardedServiceObject remaining = owners.First(o => o != removed && o.Id.ShardIndex == shardIndex);

            Assert.That(removed.Unregister(), Is.True);
            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(owners.Count - 1));
                Assert.That(removed.ServiceObjectId.IsRegistered, Is.False);
                Assert.That(removed.ServiceObjectId.Value, Is.EqualTo(ServiceObjectId.UNSET_ID));
                Assert.That(_array.Shards[shardIndex][0], Is.SameAs(remaining));
                Assert.That(remaining.ServiceObjectId.Value, Is.EqualTo(0));
                Assert.That(_array.Shards[shardIndex][1], Is.Null);
            });
        }

        [Test]
        public void Remove_UnregisteredItem_ShouldReturnFalse()
        {
            TestShardedServiceObject owner = new();

            Assert.Multiple(() =>
            {
                Assert.That(owner.Id.ShardIndex, Is.EqualTo(-1));
                Assert.That(_array.Remove(owner.Id), Is.False);
            });
        }

        [Test]
        public void Reschedule_UnregisteredItem_ShouldReturnFalse()
        {
            TestShardedServiceObject owner = new();

            Assert.Multiple(() =>
            {
                Assert.That(_array.Reschedule(owner.Id), Is.False);
                Assert.That(owner.Id.PeekAction(), Is.EqualTo(ServiceObjectId.PendingAction.None));
            });
        }

        [Test]
        public void ReRegister_ShouldReuseTheSameShard()
        {
            TestShardedServiceObject owner = new();
            owner.Register();
            _array.UpdateAndGetView(0);

            int shardIndex = owner.Id.ShardIndex;

            owner.Unregister();
            _array.UpdateAndGetView(0);
            owner.Register();
            _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(owner.Id.ShardIndex, Is.EqualTo(shardIndex));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                Assert.That(_array.Shards[shardIndex][owner.ServiceObjectId.Value], Is.SameAs(owner));
            });
        }

        [Test]
        public void Register_WithFutureTick_ShouldSleepInShardThenWake()
        {
            TestShardedServiceObject owner = new() { NextTickMs = 1000 };

            owner.Register();

            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRegistered, Is.True);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.False);
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(1000 / 30));
            });

            view = _array.UpdateAndGetView(1000);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
                Assert.That(owner.Id.SleepBucket, Is.EqualTo(-1));
            });
        }

        [Test]
        public void Reschedule_RunningItemToFuture_ShouldSleepInShardThenWake()
        {
            TestShardedServiceObject owner = new() { NextTickMs = 0 };

            owner.Register();
            _array.UpdateAndGetView(0);

            owner.NextTickMs = 600;

            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.Zero);
                Assert.That(owner.ServiceObjectId.IsRunning, Is.False);
            });

            view = _array.UpdateAndGetView(600);

            Assert.Multiple(() =>
            {
                Assert.That(view.TotalValidCount, Is.EqualTo(1));
                Assert.That(owner.ServiceObjectId.IsRunning, Is.True);
            });
        }

        [Test]
        public void ExecuteForEach_ShouldVisitAllItemsAcrossShards()
        {
            List<TestShardedServiceObject> owners = CreateOwners(_shardCount * 2);

            foreach (TestShardedServiceObject owner in owners)
                owner.Register();

            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);
            List<TestShardedServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.That(visited, Is.EquivalentTo(owners));
        }

        [Test]
        public void ExecuteForEach_AfterRemoval_ShouldVisitOnlyRemainingItems()
        {
            List<TestShardedServiceObject> owners = CreateOwners(_shardCount * 2);

            foreach (TestShardedServiceObject owner in owners)
                owner.Register();

            _array.UpdateAndGetView(0);

            TestShardedServiceObject removed = owners[0];
            removed.Unregister();
            ServiceObjectView<TestShardedServiceObject> view = _array.UpdateAndGetView(0);
            List<TestShardedServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.Multiple(() =>
            {
                Assert.That(visited, Has.Count.EqualTo(owners.Count - 1));
                Assert.That(visited, Is.EquivalentTo(owners.Where(o => o != removed)));
            });
        }

        [Test]
        public void ShardStartIndices_ShouldReflectShardSizes()
        {
            List<TestShardedServiceObject> owners = CreateOwners(_shardCount * 2);

            foreach (TestShardedServiceObject owner in owners)
                owner.Register();

            _array.UpdateAndGetView(0);

            int expectedStart = 0;

            for (int i = 0; i < _array.ShardStartIndices.Length; i++)
            {
                Assert.That(_array.ShardStartIndices[i], Is.EqualTo(expectedStart));

                int shardSize = i < _array.ShardStartIndices.Length - 1
                    ? _array.ShardStartIndices[i + 1] - _array.ShardStartIndices[i]
                    : _array.TotalValidCount - _array.ShardStartIndices[i];

                expectedStart += shardSize;
            }

            Assert.That(_array.TotalValidCount, Is.EqualTo(expectedStart));
        }

        [Test]
        public void Register_ManyObjectsConcurrently_ShouldAddThemAll()
        {
            const int count = 2000;
            List<TestShardedServiceObject> owners = CreateOwners(count);

            Parallel.For(0, count, i => owners[i].Register());
            _array.UpdateAndGetView(0);

            int[] shardCounts = owners
                .GroupBy(o => o.Id.ShardIndex)
                .Select(group => group.Count())
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(_array.TotalValidCount, Is.EqualTo(count));
                Assert.That(shardCounts.Sum(), Is.EqualTo(count));
                Assert.That(shardCounts.Max() - shardCounts.Min(), Is.LessThanOrEqualTo(1));
                Assert.That(shardCounts, Has.Length.EqualTo(_shardCount));
            });
        }

        private static List<TestShardedServiceObject> CreateOwners(int count)
        {
            List<TestShardedServiceObject> owners = new(count);

            for (int i = 0; i < count; i++)
                owners.Add(new());

            return owners;
        }
    }
}
