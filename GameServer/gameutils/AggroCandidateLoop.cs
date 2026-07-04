using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DOL.GS
{
    public ref struct AggroCandidateLoop<T>
    {
        // A helper struct to loop through all aggro candidates until _losChecksThisTick (ref) >= _maxLosChecks.
        // Because the lists returned by GetPlayersInRadius always contain elements in the same order (unless players move in or out of a subzone),
        // _index (ref) is used to rotate through the candidates, preventing the same candidates from being checked every tick.

        private readonly ReadOnlySpan<T> _targets;
        private readonly int _maxLosChecks;
        private ref int _losChecksThisTick;
        private ref int _index;
        private int _processed;

        public T Current { get; private set; }

        public AggroCandidateLoop(List<T> targets, int maxLosChecks, ref int losChecksThisTick, ref int index)
        {
            _targets = targets != null ? CollectionsMarshal.AsSpan(targets) : default;
            _maxLosChecks = maxLosChecks;
            _losChecksThisTick = ref losChecksThisTick;
            _index = ref index;
            Current = default;
        }

        public readonly AggroCandidateLoop<T> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_targets.Length == 0 || _processed >= _targets.Length || _losChecksThisTick >= _maxLosChecks)
            {
                Current = default;
                return false;
            }

            int currentIdx = (_index + _processed) % _targets.Length;
            Current = _targets[currentIdx];
            _processed++;
            return true;
        }

        public void Dispose()
        {
            if (_targets.Length > 0)
                _index = (_index + _processed) % _targets.Length;
        }
    }
}
