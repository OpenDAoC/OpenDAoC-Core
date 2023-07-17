using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("TurretsRelease")]
    public class TurretsReleaseSpellHandler : SpellHandler
    {
        public TurretsReleaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster is not GamePlayer)
                return false;

            // The main pet is automatically selected by 'SpellHandler.Tick()' and overwrites any target the player may have.
            // But we want to make Release Clump useable even when there is no main pet to target.
            if (Caster.TargetObject is TurretPet turret && turret.Owner == Caster)
            {
                Target = turret;
                selectedTarget = turret;
            }
            else
            {
                if (selectedTarget == null)
                {
                    if (Caster is GamePlayer)
                        MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "TurretsRelease.CheckBeginCast.NoSelectedTarget"), EChatType.CT_SpellResisted);

                    return false;
                }

                if (selectedTarget is not TurretPet target || target.Owner != Caster)
                {
                    if (Caster is GamePlayer)
                        MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "TurretsRelease.CheckBeginCast.NoSelectedTarget"), EChatType.CT_SpellResisted);

                    return false;
                }
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target == null || target.CurrentRegion == null)
                return;

            foreach (GameNPC npc in target.CurrentRegion.GetNPCsInRadius(target.X, target.Y, target.Z, (ushort)Spell.Radius, true))
            {
                if (npc is not TurretPet || !npc.IsAlive)
                    continue;

                if (Caster.IsControlledNPC(npc))
                    npc.Die(Caster);
            }
        }
    }
}