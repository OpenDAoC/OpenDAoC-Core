using System.Collections.Generic;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_ServiceObjectView
    {
        private ITickExecutor _tickExecutor;

        [SetUp]
        public void SetUp()
        {
            _tickExecutor = TestExecutor.Instance.TickExecutor;
        }

        [Test]
        public void FlatView_ShouldExposeItemsAndCount()
        {
            List<TestServiceObject> items = [new(), new(), new()];
            ServiceObjectView<TestServiceObject> view = new(items, 2);

            Assert.Multiple(() =>
            {
                Assert.That(view.IsSharded, Is.False);
                Assert.That(view.Items, Is.SameAs(items));
                Assert.That(view.TotalValidCount, Is.EqualTo(2));
                Assert.That(view.Shards, Is.Null);
                Assert.That(view.ShardStartIndices, Is.Null);
            });
        }

        [Test]
        public void FlatView_ExecuteForEach_ShouldVisitOnlyValidItems()
        {
            List<TestServiceObject> items = [new(), new(), new()];
            ServiceObjectView<TestServiceObject> view = new(items, 2);
            List<TestServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.That(visited, Is.EqualTo([items[0], items[1]]));
        }

        [Test]
        public void FlatView_ExecuteForEach_WithNoValidItems_ShouldNotInvokeAction()
        {
            List<TestServiceObject> items = [new()];
            ServiceObjectView<TestServiceObject> view = new(items, 0);
            int callCount = 0;

            _tickExecutor.ExecuteView(view, _ => callCount++);

            Assert.That(callCount, Is.Zero);
        }

        [Test]
        public void FlatView_ExecuteForEach_WithNegativeCount_ShouldNotInvokeAction()
        {
            List<TestServiceObject> items = [new()];
            ServiceObjectView<TestServiceObject> view = new(items, -1);
            int callCount = 0;

            _tickExecutor.ExecuteView(view, _ => callCount++);

            Assert.That(callCount, Is.Zero);
        }

        [Test]
        public void ShardedView_ShouldExposeShardsAndStartIndices()
        {
            List<TestServiceObject>[] shards = [[new(), new()], [new()]];
            int[] shardStartIndices = [0, 2];
            ServiceObjectView<TestServiceObject> view = new(shards, shardStartIndices, 3);

            Assert.Multiple(() =>
            {
                Assert.That(view.IsSharded, Is.True);
                Assert.That(view.Items, Is.Null);
                Assert.That(view.Shards, Is.SameAs(shards));
                Assert.That(view.ShardStartIndices, Is.SameAs(shardStartIndices));
                Assert.That(view.TotalValidCount, Is.EqualTo(3));
            });
        }

        [Test]
        public void ShardedView_ExecuteForEach_ShouldVisitAllShardsInOrder()
        {
            List<TestServiceObject>[] shards =
            [
                [new(), new()],
                [new()],
                [new(), new(), new()]
            ];

            int[] shardStartIndices = [0, 2, 3];
            ServiceObjectView<TestServiceObject> view = new(shards, shardStartIndices, 6);
            List<TestServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.That(visited, Is.EqualTo(
            [
                shards[0][0], shards[0][1],
                shards[1][0],
                shards[2][0], shards[2][1], shards[2][2]
            ]));
        }

        [Test]
        public void ShardedView_ExecuteForEach_ShouldSkipEmptyShards()
        {
            List<TestServiceObject>[] shards = [[new()], [], [new(), new()]];
            int[] shardStartIndices = [0, 1, 1];
            ServiceObjectView<TestServiceObject> view = new(shards, shardStartIndices, 3);
            List<TestServiceObject> visited = [];

            _tickExecutor.ExecuteView(view, visited.Add);

            Assert.That(visited, Is.EqualTo([shards[0][0], shards[2][0], shards[2][1]]));
        }

        [Test]
        public void ShardedView_ExecuteForEach_WithNoValidItems_ShouldNotInvokeAction()
        {
            List<TestServiceObject>[] shards = [[], []];
            int[] shardStartIndices = [0, 0];
            ServiceObjectView<TestServiceObject> view = new(shards, shardStartIndices, 0);
            int callCount = 0;

            _tickExecutor.ExecuteView(view, _ => callCount++);

            Assert.That(callCount, Is.Zero);
        }
    }
}
