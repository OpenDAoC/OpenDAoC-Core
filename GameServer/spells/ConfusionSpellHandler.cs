using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Confusion)]
    public class ConfusionSpellHandler : SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ConfusionSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public List<GameLiving> targetList = [];

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new ConfusionECSGameEffect(initParams);
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
