using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Daewain : HideableNpc
	{
		public Daewain() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159613);
			LoadTemplate(npcTemplate);

			DaewainBrain sbrain = new DaewainBrain();
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
	public class DaewainBrain : StandardMobBrain
	{
		public DaewainBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
		public override void Think()
		{
			if (Body.IsAlive)
			{
				bool playerNear = false;

				foreach (GamePlayer player in Body.GetPlayersInRadius(800))
				{
					if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
						playerNear = true;
				}

				HideableNpc body = (HideableNpc)Body;
				bool wasHidden = body.IsHidden;
				body.SetHidden(!playerNear);

				if (wasHidden && playerNear)
					Message.MessageToArea(Body, "A deep croak echoes from beneath the bridge as Daewain stirs from the shade.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, 2500);
			}

			if (PullFriends("DaewainBaf", 1500) > 0)
				Message.MessageToArea(Body, "Daewain croaks out a deep bellow, and his kin lumber to his aid!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

			base.Think();
		}
	}
}

