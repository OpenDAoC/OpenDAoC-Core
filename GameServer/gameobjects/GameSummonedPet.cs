using System;
using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
	public class GameSummonedPet : GameNPC
	{
		private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private bool m_targetInView;

		public override bool TargetInView
		{
			get
			{
				return m_targetInView;
			}
			set { m_targetInView = value; }
		}

		public bool CountsTowardsPetLimit { get; set; }

		public GameSummonedPet(INpcTemplate template) : base(template) { }

		public GameSummonedPet(ABrain brain) : base(brain) { }

		/// <summary>
		/// The owner of this pet
		/// </summary>
		public GameLiving Owner
		{
			get
			{
				if (Brain is IControlledBrain)
					return (Brain as IControlledBrain).Owner;

				return null;
			}
		}

		/// <summary>
		/// The root owner of this pet, the person at the top of the owner chain.
		/// </summary>
		public GameLiving RootOwner
		{
			get
			{
				if (Brain is IControlledBrain petBrain)
					return petBrain.GetLivingOwner();

				return null;
			}
		}

		/// <summary>
		/// Gets or sets the level of this NPC
		/// </summary>
		public override byte Level
		{
			get { return base.Level; }
			set
			{
				// Don't set the pet level until the owner is set
				// This skips unnecessary calls to code in base.Level
				if (Owner != null)
					base.Level = value;
			}
		}

		// Store the info we need from the summoning spell to calculate pet level.
		public double SummonSpellDamage { get; set; } = -88.0;
		public double SummonSpellValue { get; set; } = 44.0;

		/// <summary>
		/// Set the pet's level based on owner's level.  Make sure Owner brain has been assigned before calling!
		/// </summary>
		/// <returns>Did the pet's level change?</returns>
		public virtual bool SetPetLevel()
		{
			// Changing Level calls additional code, so only do it at the end
			byte newLevel = 0;

			if (SummonSpellDamage >= 0)
				newLevel = (byte)SummonSpellDamage;
			else if (!(Owner is GameSummonedPet))
				newLevel = (byte)((Owner?.Level ?? 0) * SummonSpellDamage * -0.01);
			else if (RootOwner is GameLiving summoner)
				newLevel = (byte)(summoner?.Level * SummonSpellDamage * -0.01);

			if (SummonSpellValue > 0  && newLevel > SummonSpellValue)
				newLevel = (byte)SummonSpellValue;

			if (newLevel < 1)
				newLevel = 1;

			if (Level == newLevel)
				return false;

			Level = newLevel;
			return true;
		}

		#region Spells

		/// <summary>
		/// Sort spells into specific lists
		/// </summary>
		public override void SortSpells()
		{
			if (Spells.Count < 1 || Level < 1 || this is TurretPet)
				return;

			base.SortSpells();

			if (Properties.PET_SCALE_SPELL_MAX_LEVEL > 0)
				ScaleSpells(Level);
		}

		/// <summary>
		/// Sort spells into specific lists, scaling spells by scaleLevel
		/// </summary>
		/// <param name="casterLevel">The level to scale the pet spell to, 0 to use pet level</param>
		protected virtual void ScaleSpells(int scaleLevel)
		{
			if (scaleLevel <= 0)
				scaleLevel = Level;

			// Need to make copies of spells to scale or else it will affect every other pet with the same spell on the server.
			// Enchanter, Cabalist, Spiritmaster and Theurgist pets need to have their spells scaled.
			// Note: This excludes every druid pet except the one that contains spirit in its name.
			if (Name.StartsWith("underhill") || Name.EndsWith("simulacrum") || Name.StartsWith("spirit") || this is TheurgistPet)
			{
				if (CanCastHarmfulSpells)
				{
					for (int i = 0; i < HarmfulSpells.Count; i++)
					{
						HarmfulSpells[i] = HarmfulSpells[i].Copy();
						ScaleSpell(HarmfulSpells[i], scaleLevel, Properties.PET_SCALE_SPELL_MAX_LEVEL);
					}
				}

				if (CanCastInstantHarmfulSpells)
				{
					for (int i = 0; i < InstantHarmfulSpells.Count; i++)
					{
						InstantHarmfulSpells[i] = InstantHarmfulSpells[i].Copy();
						ScaleSpell(InstantHarmfulSpells[i], scaleLevel, Properties.PET_SCALE_SPELL_MAX_LEVEL);
					}
				}

				if (CanCastHealSpells)
				{
					for (int i = 0; i < HealSpells.Count; i++)
					{
						HealSpells[i] = HealSpells[i].Copy();
						ScaleSpell(HealSpells[i], scaleLevel, Properties.PET_SCALE_SPELL_MAX_LEVEL);
					}
				}

				if (CanCastInstantHealSpells)
				{
					for (int i = 0; i < InstantHealSpells.Count; i++)
					{
						InstantHealSpells[i] = InstantHealSpells[i].Copy();
						ScaleSpell(InstantHealSpells[i], scaleLevel, Properties.PET_SCALE_SPELL_MAX_LEVEL);
					}
				}

				if (CanCastInstantMiscSpells)
				{
					for (int i = 0; i < InstantMiscSpells.Count; i++)
					{
						InstantMiscSpells[i] = InstantMiscSpells[i].Copy();
						ScaleSpell(InstantMiscSpells[i], scaleLevel, Properties.PET_SCALE_SPELL_MAX_LEVEL);
					}
				}

				if (CanCastMiscSpells)
				{
					for (int i = 0; i < MiscSpells.Count; i++)
					{
						MiscSpells[i] = MiscSpells[i].Copy();
						ScaleSpell(MiscSpells[i], scaleLevel, Properties.PET_SCALE_SPELL_MAX_LEVEL);
					}
				}
			}
		}

		/// <summary>
		/// Scale the passed spell according to PET_SCALE_SPELL_MAX_LEVEL
		/// </summary>
		/// <param name="spell">The spell to scale</param>
		/// <param name="casterLevel">The level to scale the pet spell to, 0 to use pet level</param>
		public override void ScaleSpell(Spell spell, int casterLevel, double baseLineLevel)
		{
			if (Properties.PET_SCALE_SPELL_MAX_LEVEL > 0)
				base.ScaleSpell(spell, casterLevel, baseLineLevel);
		}

		#endregion

		#region Stats
		/// <summary>
		/// Set stats according to PET_AUTOSET values, then scale them according to the npcTemplate
		/// </summary>
		public override void SetStats(DbMob dbMob = null)
		{
			// Summoned pets use their template differently from standard NPCs.
			// Stats are always automatically set based on the server properties and their level, then scaled using their template.

			Strength = Properties.PET_AUTOSET_STR_BASE;
			Constitution = Properties.PET_AUTOSET_CON_BASE;
			Dexterity = Properties.PET_AUTOSET_DEX_BASE;
			Quickness = Properties.PET_AUTOSET_QUI_BASE;
			Intelligence = Properties.PET_AUTOSET_INT_BASE;
			Empathy = 30;
			Piety = 30;
			Charisma = 30;

			if (Level > 1)
			{
				int levelMinusOne = Level - 1;
				Strength += (short) Math.Max(1, levelMinusOne * Properties.PET_AUTOSET_STR_MULTIPLIER);
				Constitution += (short) Math.Max(1, levelMinusOne * Properties.PET_AUTOSET_CON_MULTIPLIER);
				Dexterity += (short) Math.Max(1, levelMinusOne * Properties.PET_AUTOSET_DEX_MULTIPLIER);
				Quickness += (short) Math.Max(1, levelMinusOne * Properties.PET_AUTOSET_QUI_MULTIPLIER);
				Intelligence += (short) Math.Max(1, levelMinusOne * Properties.PET_AUTOSET_INT_MULTIPLIER);
			}

			if (NPCTemplate != null)
			{
				if (NPCTemplate.Strength > 0)
					Strength = (short) Math.Max(1, Strength * (NPCTemplate.Strength / 100.0));

				if (NPCTemplate.Constitution > 0)
					Constitution = (short) Math.Max(1, Constitution * (NPCTemplate.Constitution / 100.0));

				if (NPCTemplate.Dexterity > 0)
					Dexterity = (short) Math.Max(1, Dexterity * (NPCTemplate.Dexterity / 100.0));

				if (NPCTemplate.Quickness > 0)
					Quickness = (short) Math.Max(1, Quickness * (NPCTemplate.Quickness / 100.0));

				if (NPCTemplate.Intelligence > 0)
					Intelligence = (short) Math.Max(1, Intelligence * (NPCTemplate.Intelligence / 100.0));

				if (NPCTemplate.Empathy > 0)
					Empathy = NPCTemplate.Empathy;

				if (NPCTemplate.Piety > 0)
					Piety = NPCTemplate.Piety;

				if (NPCTemplate.Charisma > 0)
					Charisma = NPCTemplate.Charisma;
			}
		}
		#endregion

		public override void Die(GameObject killer)
		{
			base.Die(killer);
		}

		/// <summary>
		/// Spawn texts are in database
		/// </summary>
		protected override void BuildAmbientTexts()
		{
			base.BuildAmbientTexts();
			
			// also add the pet specific ambient texts if none found
			if (ambientTexts.Count == 0)
				ambientTexts = GameServer.Instance.NpcManager.AmbientBehaviour["pet"];
		}
	}
}
