using DOL.AI.Brain;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Text;

namespace DOL.GS
{
	public class TheurgistPet : GamePet
	{
		public TheurgistPet(INpcTemplate npcTemplate) : base(npcTemplate)
		{
			if (npcTemplate.Name.ToLower().Contains("earth"))
			{
				ScalingFactor = 27;
			}

			if (npcTemplate.Name.ToLower().Contains("air"))
			{
				ScalingFactor = 12;
			}
		}

		public override void OnAttackedByEnemy(AttackData ad) 
		{
			if (ad != null && (ad.CausesCombat || ad.IsSpellResisted))
			{
				if (castingComponent != null && castingComponent.IsCasting && castingComponent.spellHandler.CastStartTick + (CurrentSpellHandler as SpellHandler).CalculateCastingTime() / 2 < GameLoop.GameLoopTime)
				{
					InterruptTime = 0;
				}
				else
					(Brain as TheurgistPetBrain).Melee = true;
			}
		}
		

		//public override int MaxHealth => Constitution * 10;

        /// <summary>
        /// not each summoned pet 'll fire ambiant sentences
        /// let's say 10%
        /// </summary>
        protected override void BuildAmbientTexts()
		{
			base.BuildAmbientTexts();
			if (ambientTexts.Count>0)
				foreach (var at in ambientTexts)
					at.Chance /= 10;
		}

        public override void AutoSetStats()
        {
			Strength = Properties.PET_AUTOSET_STR_BASE;
			if (Strength < 1)
				Strength = 1;

			Constitution = Properties.PET_AUTOSET_CON_BASE;
			if (Constitution < 1)
				Constitution = 1;

			Quickness = Properties.PET_AUTOSET_QUI_BASE;
			if (Quickness < 1)
				Quickness = 1;

			Dexterity = Properties.PET_AUTOSET_DEX_BASE;
			if (Dexterity < 1)
				Dexterity = 1;

			Intelligence = Properties.PET_AUTOSET_INT_BASE;
			if (Intelligence < 1)
				Intelligence = 1;

			Empathy = 30;
			Piety = 30;
			Charisma = 30;

			
			if (Level > 1)
			{
				// Now add stats for levelling
				Strength += (short)Math.Round(10.0 * (Level - 1) * Properties.PET_AUTOSET_STR_MULTIPLIER);
				Constitution += (short)Math.Round((Level - 1) * Properties.PET_AUTOSET_CON_MULTIPLIER / 2);
				Quickness += (short)Math.Round((Level - 1) * Properties.PET_AUTOSET_QUI_MULTIPLIER);
				Dexterity += (short)Math.Round((Level - 1) * Properties.PET_AUTOSET_DEX_MULTIPLIER);
				Intelligence += (short)Math.Round((Level - 1) * Properties.PET_AUTOSET_INT_MULTIPLIER);
				Empathy += (short)(Level - 1);
				Piety += (short)(Level - 1);
				Charisma += (short)(Level - 1);
			}
			//base.AutoSetStats();
        }
    }
}
