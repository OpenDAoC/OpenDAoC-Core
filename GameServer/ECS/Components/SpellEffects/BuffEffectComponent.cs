namespace DOL.GS.SpellEffects
{
    public class BuffEffectComponent : IEffectComponent
    {
        public GameLiving owner;
        public bool isApplied;
        public EStat statToModify;
        public int buffValue;
        public int startTick;
        public int maxDuration;

        public ESpellEffect Type { get; set; }
        public ushort SpellEffectId { get; set; }

        public BuffEffectComponent(GameLiving owner, EStat stat, int buffValue, int maxDuration, int currentTick)
        {
            this.owner = owner;
            this.statToModify = stat;
            this.buffValue = buffValue;
            this.isApplied = false;
            this.startTick = currentTick;
            this.maxDuration = maxDuration;
            this.Type = ESpellEffect.Buff;
        }

        /* public void UpdateTimeLeft()
        {
            figure out how best to track buff durations
            should be move
        } */
    }
}
