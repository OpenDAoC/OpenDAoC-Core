using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Handler for spells that are issued by the player, but cast
    /// by his pet.
    /// </summary>
    [SpellHandler(eSpellType.PetSpell)]
    class PetSpellHandler : SpellHandler
    {
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
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "PetSpellHandler.CheckBeginCast.NoControlledBrainForCast"), eChatType.CT_SpellResisted);
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

            int powerCost = PowerCost(playerCaster);

            if (powerCost > 0)
                playerCaster.ChangeMana(playerCaster, eManaChangeType.Spell, -powerCost);

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

        public PetSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
