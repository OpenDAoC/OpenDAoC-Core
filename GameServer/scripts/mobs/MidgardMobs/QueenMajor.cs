using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class QueenMajor : HideableNpc
	{
		public QueenMajor() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157467);
			LoadTemplate(npcTemplate);

			SetHidden(true);
			QueenMajorBrain sbrain = new QueenMajorBrain();
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
	public class QueenMajorBrain : StandardMobBrain, IEncounterGateOwner
	{
		public QueenMajorBrain() : base()
		{
			AggroLevel = 80;
			AggroRange = 400;
			ThinkInterval = 1000;
			GateCounter = new(20, (kills, required) =>
			{
				if (kills == required / 2)
					Message.MessageToArea(Body, "An angry buzz rises from the nest below.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
				else if (kills == required - 1)
					Message.MessageToArea(Body, "The ground trembles as something vast stirs in the nest.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			});
		}

		public EncounterKillCounter GateCounter { get; }

		public override void Think()
		{
			HideableNpc body = (HideableNpc)Body;

			if (body.SetHidden(!GateCounter.IsOpen) && !body.IsHidden)
				Message.MessageToArea(Body, "Queen Major hauls herself up from the nest, mandibles wide!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);

			int pulledFriends = PullFriends(npc => npc.Brain is QueenMajorAddBrain, 1000);
			pulledFriends += PullFriends("QueenMajorBaf", 1000);

			if (pulledFriends > 0)
				Message.MessageToArea(Body, "Queen Major emits a shrill chittering, and her brood scuttles to her defense!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}

namespace DOL.GS
{
	public class QueenMajorAdd : EncounterGateAdd
	{
		public QueenMajorAdd() : base() { }

		protected override bool IsGateOwner(GameNPC npc) => npc is QueenMajor;

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158058);
			LoadTemplate(npcTemplate);

			QueenMajorAddBrain sbrain = new QueenMajorAddBrain();
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
	public class QueenMajorAddBrain : StandardMobBrain
	{
		public QueenMajorAddBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
	}
}
