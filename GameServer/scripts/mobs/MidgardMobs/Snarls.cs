using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class Snarls : GameNPC
	{
		public Snarls() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157490);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			//RespawnInterval = Util.Random(3600000, 7200000);

			SnarlsAdd.SnarlsAddCount = 0;
			SnarlsBrain sbrain = new SnarlsBrain();
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
	public class SnarlsBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SnarlsBrain() : base()
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
			//uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
			//uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
			//log.Warn("Current time: " + hour + ":" + minute);
			if (SnarlsAdd.SnarlsAddCount >= 3)
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
			if(HasAggro && Body.TargetObject != null)
            {
				foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is SnarlsAddBrain brain)
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
	public class SnarlsAdd : GameNPC
	{
		public SnarlsAdd() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163490);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			SnarlsAddBrain sbrain = new SnarlsAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public static int SnarlsAddCount = 0;
        public override void Die(GameObject killer)
        {
			++SnarlsAddCount;
            base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class SnarlsAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SnarlsAddBrain() : base()
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