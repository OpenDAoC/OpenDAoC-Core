using System;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Spells
{
    /// <summary>
    /// Handler for spells that are issued by the player, but cast
    /// by his pet.
    /// </summary>
    [SpellHandler("PetSpell")]
    class NecromancerPetSpellHandler : SpellHandler
    {
        /// <summary>
        /// Calculate casting time based on delve and dexterity stat bonus.
        /// Necromancers do not benefit from TrialsOfAtlantis Casting Speed Bonuses.
        /// </summary>
        /// <returns></returns>
        public override int CalculateCastingTime()
        {
            int ticks = m_spell.CastTime;
            ticks = (int) (ticks * Math.Max(m_caster.CastingSpeedReductionCap, m_caster.DexterityCastTimeReduction));

            if (ticks < m_caster.MinimumCastingSpeed)
                ticks = m_caster.MinimumCastingSpeed;

            return ticks;
        }

        /// <summary>
        /// Check if we have a pet to start with.
        /// </summary>
        /// <param name="selectedTarget"></param>
        /// <returns></returns>
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!base.CheckBeginCast(selectedTarget))
                return false;

            if (Caster.ControlledBrain == null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "PetSpellHandler.CheckBeginCast.NoControlledBrainForCast"), EChatType.CT_SpellResisted);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when spell has finished casting.
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            if (Caster is not GamePlayer playerCaster || playerCaster.ControlledBrain == null)
                return;

            // No power cost, we'll drain power on the caster when
            // the pet actually starts casting it.
            // If there is an ID, create a sub spell for the pet.

            int powerCost = PowerCost(playerCaster);

            if (powerCost > 0)
                playerCaster.ChangeMana(playerCaster, EPowerChangeType.Spell, -powerCost);

            if (playerCaster.ControlledBrain is NecromancerPetBrain petBrain && Spell.SubSpellID > 0)
            {
                Spell spell = SkillBase.GetSpellByID(Spell.SubSpellID);

                if (spell != null && spell.SubSpellID == 0)
                {
                    spell.Level = Spell.Level;
                    petBrain.OnOwnerFinishPetSpellCast(spell, SpellLine, target);
                }
            }

            if (Spell.RecastDelay > 0 && m_startReuseTimer)
            {
                foreach (Spell spell in SkillBase.GetSpellList(SpellLine.KeyName))
                {
                    if (spell.SpellType == Spell.SpellType && spell.RecastDelay == Spell.RecastDelay && spell.Group == Spell.Group)
                        Caster.DisableSkill(spell, spell.RecastDelay);
                }
            }
        }

        public NecromancerPetSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
