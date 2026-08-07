using DOL.AI.Brain;

namespace DOL.GS
{
    public abstract class EncounterGateAdd : GameNPC
    {
        protected const ushort GATE_OWNER_SEARCH_RADIUS = 4000;

        private GameNPC _gateOwner;

        protected virtual bool CountsTowardGate => true;

        protected abstract bool IsGateOwner(GameNPC npc);

        public override void ProcessDeath(GameObject killer)
        {
            if (CountsTowardGate)
            {
                if (_gateOwner == null || _gateOwner.ObjectState is not eObjectState.Active || !IsGateOwner(_gateOwner))
                {
                    _gateOwner = null;

                    foreach (GameNPC npc in GetNPCsInRadius(GATE_OWNER_SEARCH_RADIUS))
                    {
                        if (IsGateOwner(npc) && npc.Brain is IEncounterGateOwner)
                        {
                            _gateOwner = npc;
                            break;
                        }
                    }
                }

                if (_gateOwner?.Brain is IEncounterGateOwner owner)
                    owner.GateCounter?.IncrementKills();
            }

            base.ProcessDeath(killer);
        }
    }
}
