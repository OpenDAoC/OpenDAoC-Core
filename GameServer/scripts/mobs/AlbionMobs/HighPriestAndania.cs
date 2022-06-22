using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class HighPriestAndania : GameNPC
	{
		public HighPriestAndania() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12276);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			HighPriestAndaniaBrain sbrain = new HighPriestAndaniaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(2500))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_SystemWindow);
			}
		}
		public override void Die(GameObject killer)
        {
			BroadcastMessage(String.Format("The {0} says, \"The {1} vanishes and his final words linger in the air, 'You may have defeated us here, but we shall meet again someday!'\"",Name,Name));
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class HighPriestAndaniaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public HighPriestAndaniaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
			ThinkInterval = 1000;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		bool playerInRoom = false;
		bool Message = false;

		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(1500))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override void Think()
		{
			foreach(GamePlayer player in Body.GetPlayersInRadius(500))
            {
				if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
					playerInRoom = true;
            }
			if (playerInRoom)
			{
				if (changed)
				{
					Body.Flags = oldFlags;
					Body.Model = oldModel;
					changed = false;
				}
			}
			else
			{
				if (changed == false)
				{
					oldFlags = Body.Flags;
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
					Body.Flags ^= GameNPC.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					changed = true;
				}
			}
			if(!HasAggressionTable())
				Message = false;

			if(HasAggro && Body.TargetObject != null)
            {
				if (!Message)
				{
					BroadcastMessage(String.Format("The {0} shouts, 'The power of Mithra cleanses this holy place. Out! Out! I command you!\n" +
					"The {1} shouts, 'Come to me, my servants! Come and serve in the glory of Mithra!", Body.Name,Body.Name));
					Message = true;
				}
				foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "AndaniaBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			base.Think();
		}
	}
}

