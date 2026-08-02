using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Blackthorn : GameNPC
	{
		public Blackthorn() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158473);
			LoadTemplate(npcTemplate);

			BlackthornBrain sbrain = new BlackthornBrain();
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
	public class BlackthornBrain : StandardMobBrain
	{
		public BlackthornBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 500;
		}

		public override void Think()
		{
			if (PullFriends(npc => npc.PackageID == Body.PackageID && npc.Name.ToLower() == "lunantishee", 1000) > 0)
				Message.MessageToArea(Body, "The blackthorn rustles menacingly, and lunantishee swarm to its defense!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}
