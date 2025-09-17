
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Based on HealSpellHandler.cs
    /// Spell calculates a percentage of the caster's health.
    /// Heals target for the full amount, Caster loses half that amount in health.
    /// </summary>
    [SpellHandler(eSpellType.PetConversion)]
    public class PetConversionSpellHandler : SpellHandler
    {
        public override string ShortDescription => $"Releases the target and all other turrets you have summoned from the area. {Spell.Value}% of the pet's health is returned as power to the caster.";

        public PetConversionSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// Execute pet conversion spell
        /// </summary>
        public override bool StartSpell(GameLiving target)
        {
            var targets = SelectTargets(target);
            if (targets.Count <= 0)
                if (m_caster.ControlledBrain != null)
                    targets.Add(m_caster.ControlledBrain.Body);
                else
                {
                    return false;
                }
            
            int mana = 0;

            foreach (GameLiving living in targets)
            {
                ApplyEffectOnTarget(living);
                mana += (int)(living.Health * Spell.Value / 100);
            }

            int absorb = m_caster.ChangeMana(m_caster, eManaChangeType.Spell, mana);

            if (m_caster is GamePlayer)
            {
                if (absorb > 0)
                    MessageToCaster("You absorb " + absorb + " power points.", eChatType.CT_Spell);
                else
                    MessageToCaster("Your power is already full!", eChatType.CT_SpellResisted);
                ((GamePlayer)m_caster).CommandNpcRelease();
            }

            return true;
        }
    }
}
