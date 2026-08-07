using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class ZritZrit : HideableNpc
	{
		public ZritZrit() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157491);
			LoadTemplate(npcTemplate);

			SetHidden(true);
			ZritZritBrain sbrain = new ZritZritBrain();
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
	public class ZritZritBrain : StandardMobBrain, IEncounterGateOwner
	{
		public ZritZritBrain() : base()
		{
			AggroLevel = 50;
			AggroRange = 300;
			ThinkInterval = 1000;
			GateCounter = new(20, (kills, required) =>
			{
				if (kills == required / 2)
					Message.MessageToArea(Body, "Furious chittering echoes from the cracks in the rock.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
				else if (kills == required - 1)
					Message.MessageToArea(Body, "Pebbles rattle loose. Something is forcing its way out.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			});
		}

		public EncounterKillCounter GateCounter { get; }

		public override void Think()
		{
			HideableNpc body = (HideableNpc)Body;

			if (body.SetHidden(!GateCounter.IsOpen) && !body.IsHidden)
				Message.MessageToArea(Body, "Zrit-Zrit squeezes out of a crack in the rock, chittering furiously!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);

			base.Think();
		}
	}
}

namespace DOL.GS
{
	public class ZritZritAdd : EncounterGateAdd
	{
		public ZritZritAdd() : base() { }

		protected override bool IsGateOwner(GameNPC npc) => npc is ZritZrit;

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167079);
			LoadTemplate(npcTemplate);

			ZritZritAddBrain sbrain = new ZritZritAddBrain();
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
	public class ZritZritAddBrain : StandardMobBrain
	{
		public ZritZritAddBrain() : base()
		{
			ThinkInterval = 1500;
		}
	}
}
