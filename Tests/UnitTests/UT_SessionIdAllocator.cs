using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOL.Network;
using NUnit.Framework;

namespace DOL.Tests.Unit.Network
{
    [TestFixture]
    public class UT_SessionIdAllocator
    {
        [Test]
        public void Allocate_ShouldAllocateUniqueSessionIds_Concurrently()
        {
            SessionIdAllocator allocator = new();
            ConcurrentBag<ushort> sessionIds = new();
            int allocationCount = (int) (ushort.MaxValue * 1.5);

            Parallel.For(0, allocationCount, _ =>
            {
                if (allocator.TryAllocate(out ushort sessionId))
                    sessionIds.Add(sessionId);
            });

            List<ushort> uniqueIds = sessionIds.Distinct().ToList();
            Assert.That(sessionIds.Count, Is.EqualTo(uniqueIds.Count));

            foreach (ushort id in uniqueIds)
                Assert.That(id, Is.Positive);
        }

        [Test]
        public void AllocateAndFree_ShouldMaintainUniquePositiveSessionIds_Concurrently()
        {
            SessionIdAllocator allocator = new();
            ConcurrentDictionary<ushort, byte> allocatedIds = new();
            int operationCount = ushort.MaxValue + 1;
            int maxParallelism = Environment.ProcessorCount * 2;
            Random rng = new();

            Parallel.For(0, operationCount, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                // Randomly choose to allocate or free.
                if (rng.NextDouble() < 0.6)
                {
                    if (allocator.TryAllocate(out ushort sessionId))
                        allocatedIds.TryAdd(sessionId, 0);
                }
                else
                {
                    // Try to free a random allocated ID, if any.
                    ushort[] current = allocatedIds.Keys.ToArray();

                    if (current.Length > 0)
                    {
                        ushort toFree = current[rng.Next(current.Length)];
                        if (allocatedIds.TryRemove(toFree, out _))
                            allocator.Free(toFree);
                    }
                }
            });

            ushort[] finalIds = allocatedIds.Keys.ToArray();
            Assert.That(finalIds.Length, Is.EqualTo(finalIds.Distinct().Count()), "Duplicate IDs found in final allocation set.");

            foreach (ushort id in finalIds)
                Assert.That(id, Is.Positive, $"ID {id} is not positive.");
        }
    }
}
