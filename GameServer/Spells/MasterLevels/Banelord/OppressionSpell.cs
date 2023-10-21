using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.Spells
{
    //shared timer 3
    
    [SpellHandler("Oppression")]
    public class OppressionSpell : MasterLevelSpellHandling
    {
        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            return true;
        }
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            if (effect.Owner is GamePlayer)
                ((GamePlayer)effect.Owner).UpdateEncumberance();
            effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, EAttackType.Spell, Caster);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect mezz = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (mezz != null)
                mezz.Cancel(false);
            base.ApplyEffectOnTarget(target);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner is GamePlayer)
                ((GamePlayer)effect.Owner).UpdateEncumberance();
            return base.OnEffectExpires(effect, noMessages);
        }
        public OppressionSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}