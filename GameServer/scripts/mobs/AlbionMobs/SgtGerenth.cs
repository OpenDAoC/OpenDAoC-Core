using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SirGerenth : GameNPC
	{
		public SirGerenth() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12123);
			LoadTemplate(npcTemplate);

			SirGerenthBrain sbrain = new SirGerenthBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class SirGerenthBrain : StandardMobBrain
	{
		public SirGerenthBrain() : base()
		{
			AggroLevel = 40;
			AggroRange = 400;
		}
		public override void Think()
		{
			if (Body.HealthPercent <= 66 && PullFriends("SirGerenthBaf", 1500) > 0)
				Message.MessageToArea(Body, "Sir Gerenth shouts, 'Rally to me, soldiers of Albion!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}
