using System;
using DOL.GS;

namespace DOL.AI.Brain
{
    /// <summary>
    /// Implemented by brains that gate an encounter behind a number of add kills.
    /// </summary>
    public interface IEncounterGateOwner
    {
        EncounterKillCounter GateCounter { get; }
    }

    /// <summary>
    /// Tracks how many adds of a given gate have died.
    /// </summary>
    public sealed class EncounterKillCounter
    {
        private readonly Action<int, int> _onProgress;

        public EncounterKillCounter(string gateId, int requiredKills, Action<int, int> onProgress = null)
        {
            GateId = gateId;
            RequiredKills = requiredKills;
            _onProgress = onProgress;
        }

        public string GateId { get; }
        public int RequiredKills { get; }
        public int Kills { get; private set; }
        public bool IsOpen => Kills >= RequiredKills;

        public void Reset()
        {
            Kills = 0;
        }

        public static void NotifyDeath(EncounterGateAdd add)
        {
            foreach (GameNPC npc in add.GetNPCsInRadius(add.GateNotifyRadius))
            {
                if (npc.Brain is IEncounterGateOwner owner && owner.GateCounter is EncounterKillCounter counter && counter.GateId == add.GateId)
                {
                    counter.Kills++;

                    if (!counter.IsOpen)
                        counter._onProgress?.Invoke(counter.Kills, counter.RequiredKills);
                }
            }
        }
    }
}
