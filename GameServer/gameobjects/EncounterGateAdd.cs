using DOL.AI.Brain;

namespace DOL.GS
{
    public abstract class EncounterGateAdd : GameNPC
    {
        private EncounterKillCounter _counter;

        protected virtual bool CountsTowardGate => true;

        protected virtual bool IsGateOwner(GameNPC npc) => npc.Brain is IEncounterGateOwner;

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (IsGateOwner(npc) && npc.Brain is IEncounterGateOwner owner)
                {
                    _counter = owner.GateCounter;
                    break;
                }
            }

            return true;
        }

        public override void ProcessDeath(GameObject killer)
        {
            if (CountsTowardGate)
                _counter?.IncrementKills();

            base.ProcessDeath(killer);
        }
    }
}
