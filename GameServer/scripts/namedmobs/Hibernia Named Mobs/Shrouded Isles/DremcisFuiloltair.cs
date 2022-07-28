using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class DremcisFuiloltair : GameEpicBoss
	{
		public DremcisFuiloltair() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Dremcis Fuiloltair Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160146);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			DremcisFuiloltairBrain.CanSpawnStag = false;

			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			DremcisFuiloltairBrain sbrain = new DremcisFuiloltairBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is FuilslathachBrain)
					npc.RemoveFromWorld();
			}
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is BeomarbhanBrain)
					npc.Die(this);
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class DremcisFuiloltairBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DremcisFuiloltairBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public static bool CanSpawnStag = false;
		private bool CanSpawnBlobs = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnBlobs = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is FuilslathachBrain)
							npc.RemoveFromWorld();
					}
					RemoveAdds = true;
				}
			}
			if(HasAggro && Body.TargetObject != null)
            {
				RemoveAdds = false;
				if (!CanSpawnBlobs)
				{
					SpawnBlobs();
					CanSpawnBlobs = true;
				}
				if (!CanSpawnStag)
				{
					SpawnStag();
					CanSpawnStag = true;
				}
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is FuilslathachBrain brain)
					{
						if (!brain.HasAggro && target.IsAlive && target != null)
							brain.AddToAggroList(target, 10);
					}
				}
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is BeomarbhanBrain brain)
					{
						if (!brain.HasAggro && target.IsAlive && target != null)
							brain.AddToAggroList(target, 10);
					}
				}
			}
			base.Think();
		}
		private void SpawnStag()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
			{
				if (npc.Brain is BeomarbhanBrain)
					return;
			}
			Beomarbhan stag = new Beomarbhan();
			stag.X = Body.X + Util.Random(-200, 200);
			stag.Y = Body.Y + Util.Random(-200, 200);
			stag.Z = Body.Z;
			stag.Heading = Body.Heading;
			stag.CurrentRegion = Body.CurrentRegion;
			stag.AddToWorld();
		}
		private void SpawnBlobs()
        {
			foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
			{
				if (npc.Brain is FuilslathachBrain)
					return;
			}
			for (int i = 0; i < Util.Random(4,6); i++)
			{
				Fuilslathach blobs = new Fuilslathach();
				blobs.X = Body.X + Util.Random(-200, 200);
				blobs.Y = Body.Y + Util.Random(-200, 200);
				blobs.Z = Body.Z;
				blobs.Heading = Body.Heading;
				blobs.CurrentRegion = Body.CurrentRegion;
				blobs.AddToWorld();
			}
		}
	}
}
//////////////////////////////////////////////////////pet///////////////////////////////////
#region Stag pet
namespace DOL.GS
{
	public class Beomarbhan : GameEpicBoss
	{
		public Beomarbhan() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Beomarbhan Initializing...");
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158366);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = -1;
			BeomarbhanBrain sbrain = new BeomarbhanBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class BeomarbhanBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BeomarbhanBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
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

#endregion
//////////////////////////////////////////////////////adds//////////////////////////////////
#region Dremcis Adds
namespace DOL.GS
{
	public class Fuilslathach : GameNPC
	{
		public Fuilslathach() : base()
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
			Model = 932;
			Name = "fuilslathach";
			Level = (byte)Util.Random(51, 59);
			Size = (byte)Util.Random(15, 20);
			RespawnInterval = -1;
			RoamingRange = 200;

			LoadedFromScript = true;
			FuilslathachBrain sbrain = new FuilslathachBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class FuilslathachBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public FuilslathachBrain() : base()
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