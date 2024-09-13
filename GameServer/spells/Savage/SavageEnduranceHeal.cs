using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.SavageEnduranceHeal)]
    public class SavageEnduranceHeal : EnduranceHealSpellHandler
    {
        public SavageEnduranceHeal(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        protected override void RemoveFromStat(int value)
        {
            m_caster.Health -= value;
        }

        public override int PowerCost(GameLiving target)
        {
            if (m_spell.Power < 0)
                return (int) (m_caster.MaxHealth * Math.Abs(m_spell.Power) * 0.01);
            else
                return m_spell.Power;
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster.Health < PowerCost(Caster))
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SavageEnduranceHeal.CheckBeginCast.InsufficientHealth"), eChatType.CT_SpellResisted);
                return false;
            }

            return base.CheckBeginCast(Caster);
        }

        public override int CalculateEnduranceCost()
        {
            return 0;
        }
    }
}
