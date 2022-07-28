using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class QueenCliodna : GameEpicBoss
	{
		public QueenCliodna() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Queen Cliodna Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 40;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
        public override void StartAttack(GameObject target)//mob only cast
        {
        }
        #region Stats
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 400; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Queen Cliodna";
			Model = 347;
			Level = 70;
			Size = 50;
			MaxDistance = 2500;
			TetherRange = 2600;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 403, 39, 0, 0);//modelID,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 405, 39);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 404, 39);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 406, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 407, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 91, 39, 0, 0);
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 468, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Crush;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			QueenCliodnaBrain sbrain = new QueenCliodnaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class QueenCliodnaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public QueenCliodnaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Cliodna_stun))
					Body.Spells.Add(Cliodna_stun);
				if (!Body.Spells.Contains(CliodnaDD))
					Body.Spells.Add(CliodnaDD);
			}
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "CliodnaBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				if (!Body.IsCasting && !Body.IsMoving)
				{
					foreach (Spell spells in Body.Spells)
					{
						if (spells != null)
						{
							if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
								Body.StopFollowing();
							else
								Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

							Body.TurnTo(Body.TargetObject);
							if (Util.Chance(100))
							{
								GameLiving target = Body.TargetObject as GameLiving;
								if (target != null)
								{
									if (!Body.IsCasting  && !target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
										Body.CastSpell(Cliodna_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									else
										Body.CastSpell(CliodnaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								}
							}
						}
					}
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_CliodnaDD;
		private Spell CliodnaDD
		{
			get
			{
				if (m_CliodnaDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4159;
					spell.Icon = 4159;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Dark Blast";
					spell.Range = 1500;
					spell.SpellID = 11892;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_CliodnaDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CliodnaDD);
				}
				return m_CliodnaDD;
			}
		}
		private Spell m_Cliodna_stun;
		private Spell Cliodna_stun
		{
			get
			{
				if (m_Cliodna_stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4125;
					spell.Icon = 4125;
					spell.TooltipId = 4125;
					spell.Duration = 9;
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Name = "Stun";
					spell.Range = 1500;
					spell.SpellID = 11893;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					spell.DamageType = (int)eDamageType.Energy;
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Cliodna_stun = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Cliodna_stun);
				}
				return m_Cliodna_stun;
			}
		}
		#endregion
	}
}

