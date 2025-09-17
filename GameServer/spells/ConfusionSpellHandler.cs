using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Confusion)]
    public class ConfusionSpellHandler : SpellHandler
    {
        public override string ShortDescription
        {
            get
            {
                if (Spell.Value >= 0)
                    return $"Monster target has a {Spell.Value}% chance to switch which target they are fighting.";
                else
                    return $"Monster target has a 100% chance to switch which target they are fighting and a {Math.Abs(Spell.Value)}% chance to attack an ally.";
            }
        }

        public ConfusionSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public List<GameLiving> TargetList = [];

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in ECSGameEffectInitParams i) => new ConfusionECSGameEffect(i));
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.ConfusionImmunity))
            {
                MessageToCaster($"{target.Name} can't be confused!", eChatType.CT_SpellResisted);
                SendEffectAnimation(target, 0, false, 0);
                return;
            }

            base.ApplyEffectOnTarget(target);
            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }

        public override bool HasPositiveEffect => false;
    }
}
