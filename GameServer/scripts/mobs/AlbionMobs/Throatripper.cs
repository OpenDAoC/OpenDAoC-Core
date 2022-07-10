using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class Throatripper : GameNPC
	{
		public Throatripper() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12233);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			//RespawnInterval = Util.Random(3600000, 7200000);

			ThroatripperAdd.ThroatripperAddCount = 0;
			ThroatripperBrain sbrain = new ThroatripperBrain();
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
	public class ThroatripperBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ThroatripperBrain() : base()
		{
			AggroLevel = 80;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		public override void Think()
		{
			if (ThroatripperAdd.ThroatripperAddCount >= 10 && Body.CurrentRegion.IsNightTime)
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
					if (npc != null && npc.IsAlive && npc.Brain is ThroatripperAddBrain brain)
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
	public class ThroatripperAdd : GameNPC
	{
		public ThroatripperAdd() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12137);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			ThroatripperAddBrain sbrain = new ThroatripperAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public static int ThroatripperAddCount = 0;
		public override void Die(GameObject killer)
		{
			if (CurrentRegion.IsNightTime)
				++ThroatripperAddCount;
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class ThroatripperAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ThroatripperAddBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
