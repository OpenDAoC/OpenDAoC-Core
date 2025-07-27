using System;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Taunt)]
    public class TauntSpellHandler : SpellHandler
    {
        public TauntSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override void FinishSpellCast(GameLiving target)
        {
            Caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null || !target.IsAlive || target.ObjectState is GameObject.eObjectState.Active)
                return;

            SendEffectAnimation(target, 0, false, 1);
            AttackData ad = CalculateDamageToTarget(target);
            DamageTarget(ad, false);

            // Interrupt only if target is actually casting.
            if (target.IsCasting && Spell.Target is not eSpellTarget.CONE)
                target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
        }

        protected override void OnSpellNegated(GameLiving target, SpellNegatedReason reason)
        {
            base.OnSpellNegated(target, reason);

            // Interrupt only if target is actually casting.
            if (target.IsCasting && Spell.Target is not eSpellTarget.CONE)
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }

        public override void DamageTarget(AttackData ad, bool showEffectAnimation, int attackResult)
        {
            base.DamageTarget(ad, showEffectAnimation, attackResult);

            // The taunt amount is a wild guess.
            if (Spell.Value > 0 && ad.Target is GameNPC npc && npc.Brain is IOldAggressiveBrain brain)
                brain.AddToAggroList(Caster, Math.Max(1, (int) (Spell.Value * Caster.Level * 0.1)));

            m_lastAttackData = ad;
        }
    }
}
