using Core.AI;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.PlayerClass;
using Core.GS.ServerProperties;

namespace Core.GS
{
	/// <summary>
	/// The Bonedancer character class.
	/// </summary>
	public class ClassBonedancerBase : ClassMystic
	{
		/// <summary>
		/// Releases controlled object
		/// </summary>
		public override void CommandNpcRelease()
		{
			BonedancerPet subpet = Player.TargetObject as BonedancerPet;
			if (subpet != null && subpet.Brain is SubPetBrain && Player.ControlledBrain is CommanderPetBrain && (Player.ControlledBrain as CommanderPetBrain).FindPet(subpet.Brain as IControlledBrain))
			{
				Player.Notify(GameLivingEvent.PetReleased, subpet);
				CommanderPet commander = (subpet.Brain as IControlledBrain).Owner as CommanderPet;
				commander.RemoveControlledNpc(subpet.Brain as IControlledBrain);
				return;
			}

			base.CommandNpcRelease();
		}

		/// <summary>
		/// Add all spell-lines and other things that are new when this skill is trained
		/// </summary>
		/// <param name="player">player to modify</param>
		/// <param name="skill">The skill that is trained</param>
		public override void OnSkillTrained(GamePlayer player, Specialization skill)
		{
			base.OnSkillTrained(player, skill);

			// BD subpet spells can be scaled with the BD's spec as a cap, so when a BD
			//	trains, we have to re-scale spells for subpets from that spec.
			if (Properties.PET_SCALE_SPELL_MAX_LEVEL > 0
				&& Properties.PET_CAP_BD_MINION_SPELL_SCALING_BY_SPEC
				&& player.ControlledBrain != null && player.ControlledBrain.Body is GameSummonedPet pet
				&& pet.ControlledNpcList != null)
					foreach (ABrain subBrain in pet.ControlledNpcList)
						if (subBrain != null && subBrain.Body is SubPet subPet && subPet.PetSpecLine == skill.KeyName)
							subPet.SortSpells();
		}
	}
}