using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Dramacus : GameNPC
	{
		public Dramacus() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160118);
			LoadTemplate(npcTemplate);

			DramacusBrain sbrain = new DramacusBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class DramacusBrain : StandardMobBrain
	{
		public DramacusBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
		}
		public override void Think()
		{
			if (PullFriends(npc => npc.Brain is YaddaBrain, 4000) > 0)
				Message.MessageToArea(Body, "Dramacus shrieks, 'Yadda! Yadda! Come quick!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}

namespace DOL.GS
{
	public class Yadda : GameNPC
	{
		public Yadda() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168085);
			LoadTemplate(npcTemplate);

			YaddaBrain sbrain = new YaddaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class YaddaBrain : StandardMobBrain
	{
		public YaddaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
		}
		public override void Think()
		{
			if (PullFriends(npc => npc.Brain is DramacusBrain, 4000) > 0)
				Message.MessageToArea(Body, "Yadda squeals, 'Dramacus! Help help help!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}
