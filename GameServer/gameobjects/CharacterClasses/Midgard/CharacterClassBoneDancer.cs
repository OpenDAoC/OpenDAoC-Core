using DOL.AI;
using DOL.GS.PlayerClass;

namespace DOL.GS
{
    /// <summary>
    /// The Bonedancer character class.
    /// </summary>
    public class CharacterClassBoneDancer : ClassMystic
    {
        /// <summary>
        /// Add all spell-lines and other things that are new when this skill is trained
        /// </summary>
        /// <param name="player">player to modify</param>
        /// <param name="skill">The skill that is trained</param>
        public override void OnSkillTrained(GamePlayer player, Specialization skill)
        {
            base.OnSkillTrained(player, skill);

            // BD subpet spells can be scaled with the BD's spec as a cap, so when a BD trains, we have to re-scale spells for subpets from that spec.
            if (ServerProperties.Properties.PET_SCALE_SPELL_MAX_LEVEL > 0 &&
                ServerProperties.Properties.PET_CAP_BD_MINION_SPELL_SCALING_BY_SPEC &&
                player.ControlledBrain != null && player.ControlledBrain.Body is GameSummonedPet pet &&
                pet.ControlledNpcList != null)
            {
                foreach (ABrain subBrain in pet.ControlledNpcList)
                {
                    if (subBrain != null && subBrain.Body is BdSubPet subPet && subPet.PetSpecLine == skill.KeyName)
                        subPet.SortSpells();
                }
            }
        }
    }
}
