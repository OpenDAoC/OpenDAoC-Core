using DOL.GS.Keeps;

namespace DOL.GS
{
    public class NpcCastingComponent : CastingComponent
    {
        private GameNPC _npcOwner;

        private bool IsCasterGuardOrImmobile => _npcOwner is GuardCaster || _npcOwner.MaxSpeedBase == 0;

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

        public override void ClearSpellHandlers()
        {
            // Make sure NPCs don't start casting pending spells after being told to stop.
            _startSkillRequests.Clear(); // This also clears pending abilities.
            _npcOwner.ClearSpellsWaitingForLosCheck();
            base.ClearSpellHandlers();
        }

        public bool IsAllowedToFollow(GameObject target)
        {
            if (!IsCasterGuardOrImmobile)
                return true;

            if (target is not GameLiving livingTarget)
                return false;

            return livingTarget.ActiveWeaponSlot is not eActiveWeaponSlot.Distance && livingTarget.IsWithinRadius(_npcOwner, livingTarget.attackComponent.AttackRange);
        }
    }
}
