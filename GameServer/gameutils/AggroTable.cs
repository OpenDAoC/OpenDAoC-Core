using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DOL.GS
{
    public sealed class AggroTable
    {
        private static uint _nextId;

        private readonly IThreatStrategy _threatStrategy;
        private Dictionary<GameLiving, AggroAmount> _table = new();
        private Dictionary<GameLiving, AggroAmount> _tempTable; // Intended to be used by confusion spells only.
        private readonly List<KeyValuePair<GameLiving, AggroAmount>> _buffer = new();
        private readonly Lock _lock = new();
        private readonly uint _id;
        private bool _updating;

        public GameLiving LastHighestThreatInAttackRange { get; private set; }

        public AggroTable(IThreatStrategy threatStrategy)
        {
            ArgumentNullException.ThrowIfNull(threatStrategy);

            _threatStrategy = threatStrategy;
            _id = Interlocked.Increment(ref _nextId);
        }

        public bool IsEmpty()
        {
            lock (_lock)
            {
                return _table.Count == 0;
            }
        }

        public AddResult AddOrUpdate(GameLiving living, long amount)
        {
            lock (_lock)
            {
                return AddOrUpdateInternal(living, amount);
            }
        }

        public AddResult AddIfAbsent(GameLiving living, long aggroAmount)
        {
            lock (_lock)
            {
                return IsInInternal(living) ? AddResult.Ignored : AddOrUpdateInternal(living, aggroAmount);
            }
        }

        public void AddTo(AggroTable other)
        {
            if (this == other || other == null)
                return;

            Lock firstLock;
            Lock secondLock;

            if (_id < other._id)
            {
                firstLock = _lock;
                secondLock = other._lock;
            }
            else
            {
                firstLock = other._lock;
                secondLock = _lock;
            }

            lock (firstLock)
            {
                lock (secondLock)
                {
                    foreach (var pair in _table)
                    {
                        GameLiving living = pair.Key;

                        if (!other.IsInInternal(living))
                            _ = other.AddOrUpdateInternal(living, pair.Value.Base);
                    }
                }
            }
        }

        public bool Remove(GameLiving living)
        {
            lock (_lock)
            {
                return _table.Remove(living);
            }
        }

        public bool IsIn(GameLiving living)
        {
            lock (_lock)
            {
                return IsInInternal(living);
            }
        }

        public long GetBaseAggroAmount(GameLiving living)
        {
            lock (_lock)
            {
                return _table.TryGetValue(living, out AggroAmount aggroAmount) ? aggroAmount.Base : 0;
            }
        }

        public List<GameLiving> GetUnordered(int skip, int take)
        {
            List<GameLiving> list = GameLoop.GetListForTick<GameLiving>();

            if (take <= 0)
                return list;

            lock (_lock)
            {
                int currentSkip = 0;
                int currentTake = 0;

                foreach (GameLiving key in _table.Keys)
                {
                    if (currentSkip < skip)
                    {
                        currentSkip++;
                        continue;
                    }

                    list.Add(key);
                    currentTake++;

                    if (currentTake == take)
                        break;
                }
            }

            return list;
        }

        public List<GameLiving> GetOrdered(int skip, int take)
        {
            // Fairly expensive and causes an allocation. Should be called rarely.

            List<GameLiving> list = GameLoop.GetListForTick<GameLiving>();

            if (take <= 0)
                return list;

            int count;
            KeyValuePair<GameLiving, AggroAmount>[] buffer = null;

            try
            {
                lock (_lock)
                {
                    count = _table.Count;

                    if (count == 0)
                        return list;

                    buffer = ArrayPool<KeyValuePair<GameLiving, AggroAmount>>.Shared.Rent(count);
                    ((ICollection<KeyValuePair<GameLiving, AggroAmount>>) _table).CopyTo(buffer, 0);
                }

                int start = Math.Min(skip, count);
                int end = Math.Min(skip + take, count);

                if (start >= end)
                    return list;

                int k = end;
                PriorityQueue<GameLiving, long> heap = new(k);

                for (int i = 0; i < count; i++)
                {
                    long effective = buffer[i].Value.Effective;

                    if (heap.Count < k)
                        heap.Enqueue(buffer[i].Key, effective);
                    else if (heap.TryPeek(out _, out long minEffective) && effective > minEffective)
                        heap.EnqueueDequeue(buffer[i].Key, effective);
                }

                int resultCount = end - start;
                CollectionsMarshal.SetCount(list, resultCount);

                for (int i = 0; i < resultCount; i++)
                    list[resultCount - 1 - i] = heap.Dequeue();

                return list;
            }
            finally
            {
                // Clear to drop references to GameLiving. This method isn't expected to be called often.
                if (buffer != null)
                    ArrayPool<KeyValuePair<GameLiving, AggroAmount>>.Shared.Return(buffer, true);
            }
        }

        public List<(GameLiving, long)> GetDebug()
        {
            // Unoptimized. For debugging purpose only.
            List<(GameLiving, long)> orderedTable;

            lock (_lock)
            {
                orderedTable = _table
                    .OrderByDescending(static x => x.Value.Effective)
                    .Select(static x => (x.Key, x.Value.Effective)).ToList();
            }

            return orderedTable;
        }

        public bool SetTemporaryAggroTable()
        {
            lock (_lock)
            {
                if (_tempTable != null)
                    return false;

                _tempTable = _table;
                _table = new();
                return true;
            }
        }

        public bool UnsetTemporaryAggroTable()
        {
            lock (_lock)
            {
                if (_tempTable == null)
                    return false;

                _table = _tempTable;
                _tempTable = null;
                return true;
            }
        }

        public GameLiving UpdateAndGetHighestThreat(int attackRange)
        {
            if (Interlocked.Exchange(ref _updating, true) != false)
                throw new InvalidOperationException($"Concurrent {nameof(UpdateAndGetHighestThreat)} detected.");

            try
            {
                lock (_lock)
                {
                    _buffer.Clear();
                    _buffer.AddRange(_table);
                }

                TargetCandidate[] validCandidates = ArrayPool<TargetCandidate>.Shared.Rent(_buffer.Count);
                int validCandidatesIndex = 0;

                try
                {
                    GameLiving highestThreatInAttackRange = null;
                    long highestEffectiveAggroInAttackRange = -1;

                    foreach (var pair in _buffer)
                    {
                        GameLiving living = pair.Key;

                        if (_threatStrategy.ShouldBeRemoved(living))
                        {
                            lock (_lock)
                            {
                                _table.Remove(living);
                            }

                            continue;
                        }

                        if (_threatStrategy.ShouldBeIgnored(living))
                            continue;

                        AggroAmount aggroAmount = pair.Value;
                        aggroAmount.Effective = _threatStrategy.CalculateEffectiveAggro(aggroAmount.Base, living, out double distance);

                        if (distance <= attackRange && _threatStrategy.IsHigherThreat(highestEffectiveAggroInAttackRange, aggroAmount.Effective))
                        {
                            highestEffectiveAggroInAttackRange = aggroAmount.Effective;
                            highestThreatInAttackRange = living;
                        }

                        validCandidates[validCandidatesIndex++] = new(living, aggroAmount.Effective);
                    }

                    _buffer.Clear();
                    LastHighestThreatInAttackRange = highestThreatInAttackRange;

                    return _threatStrategy.SelectTarget(validCandidates.AsSpan(0, validCandidatesIndex));
                }
                finally
                {
                    ArrayPool<TargetCandidate>.Shared.Return(validCandidates);
                }
            }
            finally
            {
                Volatile.Write(ref _updating, false);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                // Don't clear _tempTable here.
                _table.Clear();
                LastHighestThreatInAttackRange = null;
            }
        }

        private AddResult AddOrUpdateInternal(GameLiving living, long amount)
        {
            bool isFirst = _table.Count == 0;

            // Always add at least 1 if the key is not present to ensure the NPC goes to the puller and not a group member.
            if (_table.TryGetValue(living, out AggroAmount aggroAmount))
            {
                aggroAmount.Base = Math.Max(0, aggroAmount.Base + amount);
                return AddResult.Updated;
            }

            _table[living] = new(Math.Max(1, amount));
            return isFirst ? AddResult.First : AddResult.New;
        }

        private bool IsInInternal(GameLiving living)
        {
            return _table.ContainsKey(living);
        }

        public enum AddResult
        {
            Ignored,
            First,
            New,
            Updated
        }

        public readonly record struct TargetCandidate(GameLiving Living, long EffectiveAggro);

        private class AggroAmount(long baseAggro = 0)
        {
            public long Base { get; set; } = baseAggro;
            public long Effective { get; set; }
        }
    }

    public interface IThreatStrategy
    {
        long CalculateEffectiveAggro(long baseAggro, GameLiving target, out double distance);
        bool IsHigherThreat(long incumbentEffectiveAggro, long challengerEffectiveAggro);
        GameLiving SelectTarget(ReadOnlySpan<AggroTable.TargetCandidate> candidates);
        bool ShouldBeRemoved(GameLiving target);
        bool ShouldBeIgnored(GameLiving target);
    }
}
