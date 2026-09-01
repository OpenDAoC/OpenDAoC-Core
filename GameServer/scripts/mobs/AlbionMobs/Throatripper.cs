using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Throatripper : HideableNpc
	{
		public Throatripper() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12233);
			LoadTemplate(npcTemplate);

			SetHidden(true);
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
	public class ThroatripperBrain : StandardMobBrain, IEncounterGateOwner
	{
		public ThroatripperBrain() : base()
		{
			AggroLevel = 80;
			AggroRange = 400;
			ThinkInterval = 1000;
			GateCounter = new(10, (kills, required) =>
			{
				if (kills == required / 2)
					Message.MessageToArea(Body, "Distant howls answer one another in the dark.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
				else if (kills == required - 1)
					Message.MessageToArea(Body, "The howling stops, and the forest falls into an eerie silence...", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			});
		}

		public EncounterKillCounter GateCounter { get; }

		public override void Think()
		{
			bool isNight = Body.CurrentRegion.IsNightTime;

			if (!isNight)
			{
				if (GateCounter.Kills > 0)
					Message.MessageToArea(Body, "The pack scatters as the sky pales; the hunt is over for tonight.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
				GateCounter.Reset();
			}

			HideableNpc body = (HideableNpc)Body;

			if (body.SetHidden(!(GateCounter.IsOpen && isNight) && !Body.InCombat) && !body.IsHidden)
				Message.MessageToArea(Body, "A shape detaches itself from the treeline. Throatripper has come.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);

			if (PullFriends(npc => npc.Brain is ThroatripperAddBrain, 1000) > 0)
				Message.MessageToArea(Body, "Throatripper's chilling howl echoes through the night!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}

namespace DOL.GS
{
	public class ThroatripperAdd : EncounterGateAdd
	{
		public ThroatripperAdd() : base() { }

		protected override bool IsGateOwner(GameNPC npc) => npc is Throatripper;
		protected override bool CountsTowardGate => CurrentRegion.IsNightTime;

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12137);
			LoadTemplate(npcTemplate);

			ThroatripperAddBrain sbrain = new ThroatripperAddBrain();
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
	public class ThroatripperAddBrain : StandardMobBrain
	{
		public ThroatripperAddBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
	}
}
