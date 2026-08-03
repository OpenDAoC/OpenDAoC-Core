using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class GiantLemer : HideableNpc
	{
		public GiantLemer() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50014);
			LoadTemplate(npcTemplate);

			SetHidden(!CurrentRegion.IsNightTime);
			GiantLemerBrain sbrain = new GiantLemerBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}

		public override void ProcessDeath(GameObject killer)
		{
			(Brain as GiantLemerBrain)?.ClearAdds();
			base.ProcessDeath(killer);
		}
	}
}

namespace DOL.AI.Brain
{
	public class GiantLemerBrain : StandardMobBrain
	{
		public GiantLemerBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
		}

		bool spawnRats = false;
		private bool RemoveAdds = false;

		public void ClearAdds()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GiantLemerAddBrain)
					npc.RemoveFromWorld();
			}
		}

		public override void Think()
		{
			HideableNpc body = (HideableNpc) Body;
			bool wasHidden = body.IsHidden;
			body.SetHidden(!Body.CurrentRegion.IsNightTime && !Body.InCombat);

			if (wasHidden && !body.IsHidden)
				Message.MessageToZone(Body.CurrentZone, "A great growl goes through the woods.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);

			if (!HasAggro)
			{
				spawnRats = false;
				if (!RemoveAdds)
				{
					ClearAdds();
					RemoveAdds = true;
				}
			}

			if(HasAggro && Body.TargetObject != null)
            {
				RemoveAdds = false;
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
			Message.MessageToArea(Body, "Squealing rats boil out of the underbrush at the giant lemer's call!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
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
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class GiantLemerAddBrain : StandardMobBrain
	{
		public GiantLemerAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
		}
	}
}
#endregion
