﻿using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class NpcCronker : GameNpc
	{
		public NpcCronker() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12329);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			//RespawnInterval = Util.Random(3600000, 7200000);

			CronkerBrain sbrain = new CronkerBrain();
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
	public class CronkerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CronkerBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
		ushort oldModel;
		GameNpc.eFlags oldFlags;
		bool changed;
		public override void Think()
		{
			uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
			//uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
			//log.Warn("Current time: " + hour + ":" + minute);
			if (hour >= 8 && hour < 14)
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
					Body.Flags ^= GameNpc.eFlags.CANTTARGET;
					Body.Flags ^= GameNpc.eFlags.DONTSHOWNAME;
					Body.Flags ^= GameNpc.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					changed = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "CronkerBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			base.Think();
		}
	}
}
