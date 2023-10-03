﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class AbomosSoultrapper : GameEpicBoss
	{
		public AbomosSoultrapper() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Abomos the Soultrapper Initializing...");
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(35))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					CastSpell(LifedrainProc, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20; // dmg reduction for melee dmg
				case eDamageType.Crush: return 20; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
			}
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
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(this.Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else//take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override double AttackDamage(DbInventoryItem weapon)
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
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157522);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 841, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Slash;

			AbomosSoultrapperBrain sbrain = new AbomosSoultrapperBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		private Spell m_LifedrainProc;
		private Spell LifedrainProc
		{
			get
			{
				if (m_LifedrainProc == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 710;
					spell.Icon = 710;
					spell.TooltipId = 710;
					spell.Value = -50;
					spell.LifeDrainReturn = 50;
					spell.Name = "Lifedrain";
					spell.Damage = 150;
					spell.Range = 350;
					spell.SpellID = 11793;
					spell.Target = eSpellTarget.ENEMY.ToString();
					spell.Type = eSpellType.Lifedrain.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_LifedrainProc = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LifedrainProc);
				}
				return m_LifedrainProc;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class AbomosSoultrapperBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AbomosSoultrapperBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				if (!RemoveAdds)
				{
					foreach (GameNPC adds in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (adds != null)
						{
							if (adds.IsAlive && adds.Brain is AbomosAddBrain)
							{
								adds.Die(adds);
							}
						}
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
				RemoveAdds = false;
			if(Body.InCombat && HasAggro && Body.HealthPercent <= 50)
            {
				SpawnAdds();
            }
			base.Think();
		}
		public void SpawnAdds()
		{
			for (int i = 0; i < 2; i++)
			{
				if (AbomosAdd.AddsCount < 3)
				{
					AbomosAdd Add1 = new AbomosAdd();
					Add1.X = Body.X;
					Add1.Y = Body.Y;
					Add1.Z = Body.Z;
					Add1.CurrentRegion = Body.CurrentRegion;
					Add1.Heading = Body.Heading;
					Add1.RespawnInterval = -1;
					Add1.AddToWorld();
				}
			}
		}
	}
}
///////////////////////////////////////////////////////////////////////////adddssss///////////////////////////////////
namespace DOL.GS
{
	public class AbomosAdd : GameNPC
	{
		public AbomosAdd() : base()
		{
		}
		public override double AttackDamage(DbInventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 5000; }
		}
		public static int AddsCount= 0;
		public override void Die(GameObject killer)
		{
			--AddsCount;
			base.Die(killer);
		}
		public override void DropLoot(GameObject killer) //no loot
		{
		}
		public override long ExperienceValue => 0;
		public override bool AddToWorld()
		{
			Model = 826;
			Name = "Abomos Servant";
			RespawnInterval = -1;
			++AddsCount;

			Size = (byte)Util.Random(80, 100);
			Level = (byte)Util.Random(50,55);
			MaxSpeedBase = 200;

			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
			Realm = eRealm.None;

			Strength = 100;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 125;
			Piety = 150;
			Intelligence = 150;

			AbomosAddBrain adds = new AbomosAddBrain();
			SetOwnBrain(adds);
			LoadedFromScript = false;
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class AbomosAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public AbomosAddBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}

		public override void Think()
		{
			base.Think();
		}
	}
}
