using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Ulfketill : GameEpicBoss
	{
		public Ulfketill() : base() { }
		public readonly List<GameNPC> Jotuns = new List<GameNPC>();

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Ulfketill Initializing...");
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

		public override int MeleeAttackRange => 350;
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167383);
			LoadTemplate(npcTemplate);
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds


			UlfketillBrain sbrain = new UlfketillBrain();
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
	public class UlfketillBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public UlfketillBrain() : base()
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
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (!HasAggro && AliveJotunCount() < 3)
			{
				SpawnJotuns();
			}
			if(HasAggro && Body.TargetObject != null)
            {
				foreach(GameNPC npc in Body.GetNPCsInRadius(2500))
                {
					if(npc != null && npc.IsAlive && npc.Brain is UlfketillAddsBrain brain)
                    {
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 10);
                    }
                }
            }
			base.Think();
		}
		private int AliveJotunCount()
		{
			if (Body is not Ulfketill ulfketill)
				return 0;

			int count = 0;
			foreach (GameNPC npc in ulfketill.Jotuns)
			{
				if (npc != null && npc.IsAlive && npc.ObjectState is GameObject.eObjectState.Active)
					++count;
			}
			return count;
		}
		private void SpawnJotuns()
		{
			if (Body is not Ulfketill ulfketill)
				return;

			ulfketill.Jotuns.RemoveAll(npc => npc == null || !npc.IsAlive || npc.ObjectState is not GameObject.eObjectState.Active);
			int jotunsCount = ulfketill.Jotuns.Count;
			for (int i = 0; i < 3; i++)
			{
				if (jotunsCount < 3)
				{
					UlfketillAdds add = new UlfketillAdds();
					add.X = Body.X + Util.Random(-500, 500);
					add.Y = Body.Y + Util.Random(-500, 500);
					add.Z = Body.Z;
					add.Heading = Body.Heading;
					add.CurrentRegion = Body.CurrentRegion;
					add.AddToWorld();
					ulfketill.Jotuns.Add(add);
					++jotunsCount;
				}
			}
		}
	}
}
////////////////////////////////////////////////////////////adds//////////////////////////////////////////
namespace DOL.GS
{
	public class UlfketillAdds : GameNPC
	{
		public UlfketillAdds() : base() { }
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
		public override void ReturnToSpawnPoint(short speed)
		{
			return;
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
			get { return 5000; }
		}
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 200; }
		public override bool AddToWorld()
		{
			Model = 1770;
			Size = (byte)Util.Random(40, 50);
			Name = "clay jotun guard";
			RespawnInterval = -1;
			Level = (byte)Util.Random(50, 55);
			MaxSpeedBase = 225;

			UlfketillAddsBrain sbrain = new UlfketillAddsBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
		public override bool CanDropLoot => false;
		public override long ExperienceValue => 0;
	}
}
namespace DOL.AI.Brain
{
	public class UlfketillAddsBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public UlfketillAddsBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Brain is UlfketillBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 10);
					}
				}
			}
			if(!HasAggro)
            {
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Brain is UlfketillBrain brain)
					{
						if (!brain.HasAggro)
							Body.Follow(npc, 150, 8000);
					}
				}
			}
			base.Think();
		}
	}
}
