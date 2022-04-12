using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Events;

namespace DOL.GS
{
	public class NorsobAnnihilator : GameEpicBoss
	{
		public NorsobAnnihilator() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Norsob the Annihilator Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 40;// dmg reduction for rest resists
			}
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (this.IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
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
			if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 700;
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
			Model = 826;
			Level = 72;
			Name = "Norsob the Annihilator";
			Size = 180;
			ParryChance = 70;

			Strength = 5;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 100;
			Piety = 200;
			Intelligence = 200;
			Empathy = 250;

			MaxSpeedBase = 250;
			MaxDistance = 2500;
			TetherRange = 1800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 446, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = eDamageType.Slash;
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
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
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
	public class SarganConqueror : GameEpicBoss
	{
		public SarganConqueror() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Sargan the Conqueror Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 40;// dmg reduction for rest resists
			}
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (this.IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
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
			if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 700;
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
			Model = 827;
			Level = 71;
			Name = "Sargan the Conqueror";
			Size = 160;
			EvadeChance = 70;

			Strength = 5;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 100;
			Piety = 200;
			Intelligence = 200;
			Empathy = 250;

			MaxSpeedBase = 250;
			MaxDistance = 2500;
			TetherRange = 1800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 1, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = eDamageType.Thrust;
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
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
			}
			base.Think();
		}
	}
}
