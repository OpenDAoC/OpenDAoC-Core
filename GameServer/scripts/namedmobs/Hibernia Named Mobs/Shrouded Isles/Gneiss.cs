using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Gneiss : GameEpicBoss
	{
		public Gneiss() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Gneiss Initializing...");
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161448);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			GneissBrain sbrain = new GneissBrain();
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
	public class GneissBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GneissBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		private bool CanSpawnAdd = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnAdd = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is GneissAddBrain)
							npc.RemoveFromWorld();
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "GneissBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				if (!CanSpawnAdd)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CreateAdd), Util.Random(60000, 120000));
					CanSpawnAdd = true;
				}
			}
			base.Think();
		}
		private int CreateAdd(ECSGameTimer timer)
		{
			if (HasAggro)
				SpawnAdd();
			CanSpawnAdd = false;
			return 0;
		}
		private void SpawnAdd()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is GneissAddBrain)
					return;
			}
			GneissAdd tree = new GneissAdd();
			tree.X = Body.X + Util.Random(-200, 200);
			tree.Y = Body.Y + Util.Random(-200, 200);
			tree.Z = Body.Z;
			tree.Heading = Body.Heading;
			tree.CurrentRegion = Body.CurrentRegion;
			tree.AddToWorld();
		}
	}
}
#region Gneiss Add
namespace DOL.GS
{
	public class GneissAdd : GameNPC
	{
		public GneissAdd() : base()
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
			get { return 2500; }
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
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		public override bool AddToWorld()
		{
			Model = 925;
			Name = "Gneiss's Piece";
			Level = 58;
			Size = (byte)Util.Random(50, 55);
			RespawnInterval = -1;
			RoamingRange = 200;

			LoadedFromScript = true;
			GneissAddBrain sbrain = new GneissAddBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class GneissAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GneissAddBrain() : base()
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