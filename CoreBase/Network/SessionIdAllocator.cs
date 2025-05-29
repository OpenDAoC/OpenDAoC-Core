using System;
using System.Threading;

namespace DOL.Network
{
    public sealed class SessionIdAllocator
    {
        private const int MAX_ID = ushort.MaxValue;
        private const int BITS_PER_LONG = 64;
        private const int ARRAY_LENGTH = (MAX_ID + 1) / BITS_PER_LONG;

        private readonly ulong[] bitSet = new ulong[ARRAY_LENGTH];
        private int nextCandidate = 0;

        public bool TryAllocate(out ushort sessionId)
        {
            int start = (Interlocked.Increment(ref nextCandidate) - 1) % MAX_ID + 1;

            for (int i = start; i < MAX_ID; i++)
            {
                int id = (start + i - 1) % MAX_ID + 1; // [1, 65535].
                int index = id / BITS_PER_LONG;
                int bit = id % BITS_PER_LONG;
                ulong mask = (ulong) 1 << bit;
                ulong oldValue;
                ulong newValue;

                do
                {
                    oldValue = Volatile.Read(ref bitSet[index]);

                    if ((oldValue & mask) != 0)
                        break; // This ID is already taken; try the next candidate.

                    newValue = oldValue | mask;
                } while (Interlocked.CompareExchange(ref bitSet[index], newValue, oldValue) != oldValue);

                if ((oldValue & mask) == 0)
                {
                    sessionId = (ushort) id;
                    return true;
                }
            }

            sessionId = 0;
            return false; // No IDs available.
        }

        public void Free(ushort sessionId)
        {
            if (sessionId == 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId), "Session ID is out of range.");

            int index = sessionId / BITS_PER_LONG;
            int bit = sessionId % BITS_PER_LONG;
            ulong mask = ~((ulong) 1 << bit);
            ulong oldValue;
            ulong newValue;

            do
            {
                oldValue = Volatile.Read(ref bitSet[index]);
                newValue = oldValue & mask;
            } while (Interlocked.CompareExchange(ref bitSet[index], newValue, oldValue) != oldValue);
        }
    }
}
