using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Snarls : HideableNpc
	{
		public Snarls() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157490);
			LoadTemplate(npcTemplate);

			SetHidden(true);
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
	public class SnarlsBrain : StandardMobBrain, IEncounterGateOwner
	{
		public SnarlsBrain() : base()
		{
			AggroLevel = 80;
			AggroRange = 400;
			ThinkInterval = 1000;
		}

		public EncounterKillCounter GateCounter { get; } = new("SnarlsGate", 3);

		public override void Think()
		{
			HideableNpc body = (HideableNpc)Body;

			if (body.SetHidden(!GateCounter.IsOpen) && !body.IsHidden)
				Message.MessageToArea(Body, "Snarls pads out of the treeline, hackles raised.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);

			if (PullFriends(npc => npc.Brain is SnarlsAddBrain, 1000) > 0)
				Message.MessageToArea(Body, "Snarls lets loose a ferocious growl, calling the pack to the hunt!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}

namespace DOL.GS
{
	public class SnarlsAdd : EncounterGateAdd
	{
		public SnarlsAdd() : base() { }

		public override string GateId => "SnarlsGate";

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163490);
			LoadTemplate(npcTemplate);

			SnarlsAddBrain sbrain = new SnarlsAddBrain();
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
	public class SnarlsAddBrain : StandardMobBrain
	{
		public SnarlsAddBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
	}
}
