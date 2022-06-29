using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class Develin : GameEpicNPC
	{
		public Develin() : base() { }

		public static int KillsRequireToSpawn = 20;
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is DevelinBrain)
					return false;
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159930);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			KillsRequireToSpawn = Util.Random(20, 40);
			//log.Warn("KillsRequireToSpawn = " + KillsRequireToSpawn);

			DevelinAdd.DevelinAddCount = 0;
			DevelinBrain sbrain = new DevelinBrain();
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
	public class DevelinBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DevelinBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		public override void Think()
		{
			if (DevelinAdd.DevelinAddCount >= Develin.KillsRequireToSpawn && Body.CurrentRegion.IsNightTime)
			{
				if (changed)
				{
					Body.Flags = oldFlags;
					Body.Model = oldModel;
					changed = false;
				}
			}
			else
			{
				if (changed == false)
				{
					oldFlags = Body.Flags;
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
					Body.Flags ^= GameNPC.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					changed = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is DevelinAddBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
							brain.AddToAggroList(target, 10);
					}
				}
			}
			base.Think();
		}
	}
}

namespace DOL.GS
{
	public class DevelinAdd : GameNPC
	{
		public DevelinAdd() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164991);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			DevelinAddBrain sbrain = new DevelinAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public static int DevelinAddCount = 0;
		public override void Die(GameObject killer)
		{
			if (CurrentRegion.IsNightTime)
				++DevelinAddCount;
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class DevelinAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DevelinAddBrain() : base()
		{
			AggroLevel = 80;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}

