using System;
using System.Threading;

namespace DOL.AI.Brain
{
    public interface IEncounterGateOwner
    {
        EncounterKillCounter GateCounter { get; }
    }

    public sealed class EncounterKillCounter
    {
        private readonly Action<int, int> _onProgress;
        private int _kills;

        public EncounterKillCounter(int requiredKills, Action<int, int> onProgress = null)
        {
            RequiredKills = requiredKills;
            _onProgress = onProgress;
        }

        public int RequiredKills { get; }
        public int Kills => Volatile.Read(ref _kills);
        public bool IsOpen => Kills >= RequiredKills;

        public void Reset()
        {
            Interlocked.Exchange(ref _kills, 0);
        }

        public void IncrementKills()
        {
            int newKills = Interlocked.Increment(ref _kills);

            if (newKills < RequiredKills)
                _onProgress?.Invoke(newKills, RequiredKills);
        }
    }
}
