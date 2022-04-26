using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class UlorBysen : GameEpicBoss
	{
		public UlorBysen() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Ulor se Bysen Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
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
			return 600;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.45;
		}
        public override void StartAttack(GameObject target)
        {
        }
        public override int MaxHealth
		{
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			Model = 919;
			Level = (byte)(Util.Random(72,77));
			Name = "Ulor se Bysen";
			Size = 120;

			Dexterity = 200;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Crush;
			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			UlorBysenBrain sbrain = new UlorBysenBrain();
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
	public class UlorBysenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public UlorBysenBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Ulor_DD))
					Body.Spells.Add(Ulor_DD);
				if (!Body.Spells.Contains(Ulor_aoedot))
					Body.Spells.Add(Ulor_aoedot);
				if (!Body.Spells.Contains(Ulor_DebuffBody))
					Body.Spells.Add(Ulor_DebuffBody);
			}
			if (HasAggro)
			{

				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is JehrogBysenBrain brain)
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (!brain.HasAggro && target != null && target.IsAlive)
								brain.AddToAggroList(target, 10);
						}
					}
				}
				if (Body.TargetObject != null)
				{
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
									if (Body.GetSkillDisabledDuration(Ulor_aoedot) == 0)
										Body.CastSpell(Ulor_aoedot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									else
									{
										if (Body.GetSkillDisabledDuration(Ulor_DebuffBody) == 0)
											Body.CastSpell(Ulor_DebuffBody, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
										else
											Body.CastSpell(Ulor_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									}
								}
							}
						}
					}
				}
			}
			base.Think();
		}
        #region spells
        private Spell m_Ulor_DD;
		private Spell Ulor_DD
		{
			get
			{
				if (m_Ulor_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 662;
					spell.Icon = 662;
					spell.TooltipId = 662;
					spell.Damage = 400;
					spell.Name = "Essence Devourer";
					spell.Range = 1500;
					spell.Radius = 200;
					spell.SpellID = 11886;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Body;
					m_Ulor_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Ulor_DD);
				}
				return m_Ulor_DD;
			}
		}
		private Spell m_Ulor_DebuffBody;
		private Spell Ulor_DebuffBody
		{
			get
			{
				if (m_Ulor_DebuffBody == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.RecastDelay = 15;
					spell.ClientEffect = 879;
					spell.Icon = 879;
					spell.TooltipId = 879;
					spell.Value = 50;
					spell.Duration = 8;
					spell.Name = "Banish Immunities";
					spell.Description = "Decreases a target's given resistance to Body magic by 50";
					spell.Message1 = "You feel more vulnerable to physical magic!";
					spell.Message2 = "{0} seems vulnerable to physical magic!";
					spell.Message3 = "Your physical vulnerability fades.";
					spell.Message4 = "{0}'s physical vulnerability fades.";
					spell.Range = 1500;
					spell.SpellID = 11888;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.BodyResistDebuff.ToString();
					spell.DamageType = (int)eDamageType.Body;
					m_Ulor_DebuffBody = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Ulor_DebuffBody);
				}
				return m_Ulor_DebuffBody;
			}
		}
		private Spell m_Ulor_aoedot;
		private Spell Ulor_aoedot
		{
			get
			{
				if (m_Ulor_aoedot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 585;
					spell.Icon = 585;
					spell.TooltipId = 585;
					spell.Damage = 83;
					spell.Duration = 24;
					spell.Frequency = 40;
					spell.Name = "Lance Spirit";
					spell.Description = "Inflicts 83 damage to the target every 4 sec for 24 seconds";
					spell.Message1 = "Your body is covered with painful sores!";
					spell.Message2 = "{0}'s skin erupts in open wounds!";
					spell.Message3 = "The destructive energy wounding you fades.";
					spell.Message4 = "The destructive energy around {0} fades.";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11887;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.DamageType = (int)eDamageType.Matter;
					m_Ulor_aoedot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Ulor_aoedot);
				}
				return m_Ulor_aoedot;
			}
		}
        #endregion
    }
}
//////////////////////////////////////////////////////////////////////Jehrog//////////////////////////////////////////////////////
namespace DOL.GS
{
	public class JehrogBysen : GameEpicBoss
	{
		public JehrogBysen() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Jehrog se Bysen Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
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
			return 600;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.45;
		}
		public override int MaxHealth
		{
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			Model = 919;
			Level = (byte)(Util.Random(72, 77));
			Name = "Jehrog se Bysen";
			Size = 120;

			Strength = 700;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 842, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Crush;
			ParryChance = 60;

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			JehrogBysenBrain sbrain = new JehrogBysenBrain();
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
	public class JehrogBysenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public JehrogBysenBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is UlorBysenBrain brain)
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (!brain.HasAggro && target != null && target.IsAlive)
								brain.AddToAggroList(target, 10);
						}
					}
				}
			}
			base.Think();
		}
	}
}