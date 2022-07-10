using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class GreenMaw : GameEpicBoss
	{
		public GreenMaw() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Green Maw Initializing...");
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
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is GreenMawBrain)
					return false;
			}
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GreenMawAddBrain)
					npc.RemoveFromWorld();
			}
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GreenMawAdd2Brain)
					npc.RemoveFromWorld();
			}
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GreenMawAdd3Brain)
					npc.RemoveFromWorld();
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50022);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			GreenMawAdd.GreenMawRedCount = 0;
			GreenMawAdd2.GreenMawOrangeCount = 0;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			GreenMawBrain sbrain = new GreenMawBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			SpawnCopies();
			base.Die(killer);
		}
		private void SpawnCopies()
		{
			for (int i = 0; i < 3; i++)
			{
				GreenMawAdd npc = new GreenMawAdd();
				npc.X = X + Util.Random(-50, 50);
				npc.Y = Y + Util.Random(-50, 50);
				npc.Z = Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 450;
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
			base.Think();
		}
	}
}

#region Green maw Copies Red
namespace DOL.GS
{
	public class GreenMawAdd : GameNPC
	{
		public GreenMawAdd() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 250;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 6000; }
		}
		#region Stats
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 200; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Part of Green Maw";
			Level = (byte)Util.Random(58,60);
			Model = 136;
			Size = 120;
			GreenMawAddBrain sbrain = new GreenMawAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		public static int GreenMawRedCount = 0;
        public override void Die(GameObject killer)
        {
			++GreenMawRedCount;
			if (GreenMawRedCount >= 3)
				SpawnCopies();
			base.Die(killer);
        }
		public override void DropLoot(GameObject killer) //no loot
		{
		}
		private void SpawnCopies()
		{
			for (int i = 0; i < 4; i++)
			{
				GreenMawAdd2 npc = new GreenMawAdd2();
				npc.X = X + Util.Random(-50, 50);
				npc.Y = Y + Util.Random(-50, 50);
				npc.Z = Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

#region Green maw Copies Orange
namespace DOL.GS
{
	public class GreenMawAdd2 : GameNPC
	{
		public GreenMawAdd2() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 4000; }
		}
		#region Stats
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Part of Green Maw";
			Level = (byte)Util.Random(53, 55);
			Model = 136;
			Size = 95;
			GreenMawAdd2Brain sbrain = new GreenMawAdd2Brain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		public static int GreenMawOrangeCount = 0;
		public override void Die(GameObject killer)
		{
			++GreenMawOrangeCount;
			if (GreenMawOrangeCount >= 4)
				SpawnCopies();
			base.Die(killer);
		}
		public override void DropLoot(GameObject killer) //no loot
		{
		}
		private void SpawnCopies()
		{
			for (int i = 0; i < 2; i++)
			{
				GreenMawAdd3 npc = new GreenMawAdd3();
				npc.X = X + Util.Random(-50, 50);
				npc.Y = Y + Util.Random(-50, 50);
				npc.Z = Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawAdd2Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawAdd2Brain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

#region Green maw Copies Yellow
namespace DOL.GS
{
	public class GreenMawAdd3 : GameNPC
	{
		public GreenMawAdd3() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
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

		public override double GetArmorAF(eArmorSlot slot)
		{
			return 150;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 2500; }
		}
		#region Stats
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Part of Green Maw";
			Level = 50;
			Model = 136;
			Size = 70;
			GreenMawAdd3Brain sbrain = new GreenMawAdd3Brain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		public override void DropLoot(GameObject killer) //no loot
		{
		}
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawAdd3Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawAdd3Brain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion