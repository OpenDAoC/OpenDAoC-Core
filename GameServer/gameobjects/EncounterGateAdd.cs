using DOL.AI.Brain;

namespace DOL.GS
{
    /// <summary>
    /// An add whose death is reported to every nearby brain owning a matching encounter gate counter.
    /// </summary>
    public abstract class EncounterGateAdd : GameNPC
    {
        public virtual string GateId => PackageID;
        public virtual ushort GateNotifyRadius => 4000;
        protected virtual bool CountsTowardGate => true;

        public override void Die(GameObject killer)
        {
            if (CountsTowardGate)
                EncounterKillCounter.NotifyDeath(this);

            base.Die(killer);
        }
    }
}
