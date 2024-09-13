using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Amnesia)]
    public class AmnesiaSpellHandler : SpellHandler
    {
        public AmnesiaSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new AmnesiaECSEffect(initParams);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);

            if (target == null || !target.IsAlive)
                return;

            SendEffectAnimation(target, 0, false, 1);

            if (target is GamePlayer player)
            {
                player.styleComponent.NextCombatStyle = null;
                player.styleComponent.NextCombatBackupStyle = null;
            }

            // Amnesia only affects normal spells and not song activation (still affects pulses from songs though)
            if (target.CurrentSpellHandler?.Spell.InstrumentRequirement == 0)
                target.castingComponent.ClearUpSpellHandlers();

            target.rangeAttackComponent.AutoFireTarget = null;

            if (target is GamePlayer)
                MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer).Client, "Amnesia.MessageToTarget"), eChatType.CT_Spell);

        }

        /// <summary>
        /// When spell was resisted
        /// </summary>
        /// <param name="target">the target that resisted the spell</param>
        protected override void OnSpellResisted(GameLiving target)
        {
            base.OnSpellResisted(target);

            // Start interrupt even for resisted instant amnesia.
            if (Spell.CastTime == 0)
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }
    }
}
