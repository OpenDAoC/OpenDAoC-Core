using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class ScoutArgyle : GameNPC
	{
		public ScoutArgyle() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165671);
			LoadTemplate(npcTemplate);

			ScoutArgyleBrain sbrain = new ScoutArgyleBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class ScoutArgyleBrain : StandardMobBrain
	{
		public ScoutArgyleBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null && PullFriends("ScoutArgyleBaf", 1000) > 0)
				Message.MessageToArea(Body, "Scout Argyle shouts, 'Intruders in the camp! To arms!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Think();
		}
	}
}
