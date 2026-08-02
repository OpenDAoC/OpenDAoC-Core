using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SgtCosworth : GameNPC
	{
		public SgtCosworth() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12122);
			LoadTemplate(npcTemplate);

			SgtCosworthBrain sbrain = new SgtCosworthBrain();
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
	public class SgtCosworthBrain : StandardMobBrain
	{
		public SgtCosworthBrain() : base()
		{
			AggroLevel = 40;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
		public override void Think()
		{
			if (Body.HealthPercent <= 66 && PullFriends("SgtCosworthBaf", 1500) > 0)
				Message.MessageToArea(Body, "Sergeant Cosworth bellows, 'To arms, men! To arms!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}
