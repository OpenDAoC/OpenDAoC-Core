using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DOL.GS;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DOL.Tests.Unit.GameUtils.Collections
{
    [TestFixture]
    public class UT_DrainArray
    {
        [Test]
        public void DrainTo_ShouldCallAction_OncePerItem()
        {
            DrainArray<int> drainArray = new();
            int itemCount = 1000;
            ConcurrentBag<int> called = new();

            for (int i = 0; i < itemCount; i++)
                drainArray.Add(i);

            drainArray.DrainTo(called.Add);
            Assert.That(called.Count, Is.EqualTo(itemCount));
            CollectionAssert.AreEquivalent(Enumerable.Range(0, itemCount), called);
        }

        [Test]
        public void Add_ShouldBeThreadSafe_AndAllowConcurrentAdds()
        {
            DrainArray<int> drainArray = new();
            int itemCount = 10000;
            ConcurrentBag<int> added = new();

            Parallel.For(0, itemCount, i =>
            {
                drainArray.Add(i);
                added.Add(i);
            });

            ConcurrentBag<int> drained = new();
            drainArray.DrainTo(drained.Add);
            Assert.That(drained.Count, Is.EqualTo(itemCount));
            CollectionAssert.AreEquivalent(added, drained);
        }

        [Test]
        public void Add_DuringDrainTo_ShouldThrowInvalidOperationException()
        {
            DrainArray<int> drainArray = new();
            drainArray.Add(1);

            ManualResetEventSlim started = new(false);
            ManualResetEventSlim readyToAdd = new(false);
            Exception addException = null;

            // DrainTo will block until we try to Add.
            var drainTask = Task.Run(() =>
            {
                drainArray.DrainTo(i =>
                {
                    started.Set(); // Signal that DrainTo is in progress.
                    readyToAdd.Wait(); // Wait until Add is attempted.
                });
            });

            started.Wait(); // Wait for DrainTo to start.

            // Try to Add while DrainTo is in progress.
            var addTask = Task.Run(() =>
            {
                try
                {
                    drainArray.Add(42);
                }
                catch (Exception ex)
                {
                    addException = ex;
                }
                finally
                {
                    readyToAdd.Set();
                }
            });

            Task.WaitAll(drainTask, addTask);
            Assert.That(addException, Is.Not.Null);
            Assert.That(addException, Is.TypeOf<InvalidOperationException>());
        }
    }
}
