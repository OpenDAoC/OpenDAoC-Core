﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class BossNorsobAnnihilator : GameEpicBoss
	{
		public BossNorsobAnnihilator() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Norsob the Annihilator Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20; // dmg reduction for melee dmg
				case EDamageType.Crush: return 20; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
						|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
						|| damageType == EDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
		public override bool AddToWorld()
		{
			Model = 826;
			Level = 72;
			Name = "Norsob the Annihilator";
			Size = 180;
			ParryChance = 50;

			Strength = 550;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 180;
			Piety = 200;
			Intelligence = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 2500;
			TetherRange = 1800;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 446, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = EDamageType.Slash;
			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));

			NorsobAnnihilatorBrain sbrain = new NorsobAnnihilatorBrain();
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
	public class NorsobAnnihilatorBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public NorsobAnnihilatorBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
			}
			base.Think();
		}
	}
}

/// <summary>
/// ///////////////////////////////////////////////////////////////Sargan the Conqueror//////////////////////////////////////////////////  
/// </summary>
namespace DOL.GS
{
	public class BossSarganConqueror : GameEpicBoss
	{
		public BossSarganConqueror() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Sargan the Conqueror Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20; // dmg reduction for melee dmg
				case EDamageType.Crush: return 20; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
						|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
						|| damageType == EDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
		public override bool AddToWorld()
		{
			Model = 827;
			Level = 71;
			Name = "Sargan the Conqueror";
			Size = 160;
			EvadeChance = 50;

			Strength = 550;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 180;
			Piety = 200;
			Intelligence = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 2500;
			TetherRange = 1800;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 1, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = EDamageType.Thrust;
			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));

			SarganConquerorBrain sbrain = new SarganConquerorBrain();
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
	public class SarganConquerorBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SarganConquerorBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
			}
			base.Think();
		}
	}
}