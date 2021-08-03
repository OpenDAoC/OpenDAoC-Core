namespace DOL.GS.SpellEffects
{
    public class DebuffEffectComponent : IEffectComponent
    {
        public GameLiving owner;
        public bool isApplied;
        public eStat statToModify;
        public int debuffValue;
        public int startTick;
        public int maxDuration;

        public eSpellEffect Type { get; set; }
        public ushort SpellEffectId { get; set; }

        public DebuffEffectComponent(GameLiving owner, eStat stat, int debuffValue, int maxDuration, int startTick)
        {
            this.owner = owner;
            this.statToModify = stat;
            this.debuffValue = debuffValue;
            this.isApplied = false;
            this.startTick = startTick;
            this.maxDuration = maxDuration;
        }

        /* public void UpdateTimeLeft()
        {
            figure out how best to track buff durations
            should be moved to a system
        } */
    }
}
