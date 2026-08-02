using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class HighPriestAndania : HideableNpc
	{
		public HighPriestAndania() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12276);
			LoadTemplate(npcTemplate);

			SetHidden(true);
			HighPriestAndaniaBrain sbrain = new HighPriestAndaniaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}

		public override void Die(GameObject killer)
		{
			Message.MessageToArea(this, $"{Name} cries out and vanishes, his final words lingering in the air, 'You may have defeated us here, but we shall meet again someday!'", eChatType.CT_Say, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class HighPriestAndaniaBrain : StandardMobBrain
	{
		bool Message = false;

		public HighPriestAndaniaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
			ThinkInterval = 1000;
		}

		public override void Think()
		{
			bool playerInRoom = false;

			foreach (GamePlayer player in Body.GetPlayersInRadius(500))
			{
				if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
					playerInRoom = true;
			}

			((HideableNpc)Body).SetHidden(!playerInRoom);

			if (!HasAggro)
				Message = false;

			if (HasAggro && Body.TargetObject != null)
			{
				if (!Message)
				{
					DOL.GS.Message.MessageToArea(Body, $"{Body.Name} shouts, 'The power of Mithra cleanses this holy place. Out! Out! I command you!'\n" +
						$"{Body.Name} shouts, 'Come to me, my servants! Come and serve in the glory of Mithra!'", eChatType.CT_Say, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
					Message = true;
				}

				PullFriends("AndaniaBaf", 1500);
			}

			base.Think();
		}
	}
}
