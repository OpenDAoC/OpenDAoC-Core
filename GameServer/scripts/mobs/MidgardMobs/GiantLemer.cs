using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class GiantLemer : GameNPC
	{
		public GiantLemer() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50014);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			GiantLemerBrain sbrain = new GiantLemerBrain();
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
				if (npc != null && npc.IsAlive && npc.Brain is GiantLemerAddBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class GiantLemerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GiantLemerBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
			ThinkInterval = 1000;
		}

		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		bool spawnRats = false;
		public void BroadcastMessage(String message)
		{
			foreach (GameClient player in WorldMgr.GetClientsOfZone(Body.CurrentZone.ID))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override void Think()
		{
			if (Body.CurrentRegion.IsNightTime == false)
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
			if (Body.CurrentRegion.IsNightTime)
			{
				if (changed)
				{
					Body.Flags = oldFlags;
					Body.Model = oldModel;
					BroadcastMessage("A great growl goes through the woods.");
					changed = false;
				}

			}
			if (!HasAggressionTable())
			{
				spawnRats = false;
				foreach(GameNPC npc in Body.GetNPCsInRadius(5000))
                {
					if (npc != null && npc.IsAlive && npc.Brain is GiantLemerAddBrain)
						npc.RemoveFromWorld();
                }
			}

			if(HasAggro && Body.TargetObject != null)
            {
				if(!spawnRats)
                {
					SpawnRats();
					spawnRats = true;
                }
				foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
				{
					if (npc != null && npc.IsAlive && npc.Brain is GiantLemerAddBrain brain)
                    {
						GameLiving target = Body.TargetObject as GameLiving;
						if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
							brain.AddToAggroList(target, 10);
                    }
				}
			}
			base.Think();
		}
		private void SpawnRats()
		{
			for (int i = 0; i < Util.Random(2,4); i++)
			{
				GiantLemerAdd npc = new GiantLemerAdd();
				npc.X = Body.X + Util.Random(-100, 100);
				npc.Y = Body.Y + Util.Random(-100, 100);
				npc.Z = Body.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}

#region Giant lemer adds
namespace DOL.GS
{
	public class GiantLemerAdd : GameNPC
	{
		public GiantLemerAdd() : base() { }
		public override int MaxHealth
		{
			get { return 300; }
		}
		public override bool AddToWorld()
		{
			Name = "small rat";
			Level = (byte)Util.Random(13, 16);
			Model = 567;
			Size = 20;
			GiantLemerAddBrain sbrain = new GiantLemerAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class GiantLemerAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GiantLemerAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion