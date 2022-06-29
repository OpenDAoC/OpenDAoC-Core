using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Balere : GameEpicBoss
	{
		public Balere() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Balere Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
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
		public override bool AddToWorld()
		{
			Name = "Balere";
			Model = 904;
			Size = 120;
			Level = 65;
			MaxDistance = 2500;
			TetherRange = 2600;
			Faction = FactionMgr.GetFactionByID(96);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			BalereBrain sbrain = new BalereBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			foreach (GameNPC npc in GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.Brain is BalereAddBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class BalereBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BalereBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		private bool spawnAdds = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				spawnAdds = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
					{
						if (npc != null && npc.IsAlive && npc.Brain is BalereAddBrain)
							npc.RemoveFromWorld();
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if (!spawnAdds)
				{
					SpawnAdds();
					spawnAdds = true;
				}
			}
			base.Think();
		}
		private void SpawnAdds()
		{
			for (int i = 0; i < Util.Random(7, 8); i++)
			{
				BalereAdd add = new BalereAdd();
				add.X = Body.X + Util.Random(-300, 300);
				add.Y = Body.Y + Util.Random(-300, 300);
				add.Z = Body.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
		}
	}
}

#region Balere's adds
namespace DOL.GS
{
	public class BalereAdd : GameNPC
	{
		public BalereAdd() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get { return 2000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		#endregion

		public override bool AddToWorld()
		{
			Model = 904;
			Name = "young octonoid";
			Level = (byte)Util.Random(38, 44);
			Size = (byte)Util.Random(35, 45);
			RespawnInterval = -1;
			RoamingRange = 200;
			MaxDistance = 2500;
			TetherRange = 2600;
			Faction = FactionMgr.GetFactionByID(96);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

			LoadedFromScript = true;
			BalereAddBrain sbrain = new BalereAddBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class BalereAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BalereAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

