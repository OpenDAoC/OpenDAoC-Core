namespace DOL.GS
{
    public class NpcCastingComponent : CastingComponent
    {
        private GameNPC _npcOwner;

        public NpcCastingComponent(GameNPC npcOwner) : base(npcOwner)
        {
            _npcOwner = npcOwner;
        }

        public override void OnSpellCast(Spell spell)
        {
            if (!spell.IsHarmful || !spell.IsInstantCast)
                return;

            _npcOwner.ApplyInstantHarmfulSpellDelay();
        }

        public override void ClearUpSpellHandlers()
        {
            // Make sure NPCs don't start casting pending spells after being told to stop.
            _startSkillRequests.Clear(); // This also clears pending abilities.
            _npcOwner.ClearSpellsWaitingForLosCheck();
            base.ClearUpSpellHandlers();
        }
    }
}
