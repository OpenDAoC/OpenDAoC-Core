using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Stripe : HideableNpc
	{
		public Stripe() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157492);
			LoadTemplate(npcTemplate);

			SetHidden(true);
			StripeBrain sbrain = new StripeBrain();
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
	public class StripeBrain : StandardMobBrain, IEncounterGateOwner
	{
		public StripeBrain() : base()
		{
			AggroLevel = 50;
			AggroRange = 300;
			ThinkInterval = 1000;
			GateCounter = new(20, (kills, required) =>
			{
				if (kills == required / 2)
					Message.MessageToArea(Body, "The grass sways! Something big is circling closer...", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
				else if (kills == required - 1)
					Message.MessageToArea(Body, "A low snarl rolls out of the grass, very nearby.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			});
		}

		public EncounterKillCounter GateCounter { get; }

		public override void Think()
		{
			HideableNpc body = (HideableNpc)Body;

			if (body.SetHidden(!GateCounter.IsOpen) && !body.IsHidden)
				Message.MessageToArea(Body, "Stripe slinks out of the tall grass, teeth bared.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);

			base.Think();
		}
	}
}

namespace DOL.GS
{
	public class StripeAdd : EncounterGateAdd
	{
		public StripeAdd() : base() { }

		protected override bool IsGateOwner(GameNPC npc) => npc is Stripe;

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168027);
			LoadTemplate(npcTemplate);

			StripeAddBrain sbrain = new StripeAddBrain();
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
	public class StripeAddBrain : StandardMobBrain
	{
		public StripeAddBrain() : base()
		{
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
