using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class Daewain : GameNPC
	{
		public Daewain() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159613);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

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
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DaewainBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		bool playerOnBridge = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override void Think()
		{
			if(Body.IsAlive)
            {
				foreach (GamePlayer player in Body.GetPlayersInRadius(800))
				{
					if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
						playerOnBridge = true;
				}
				if (playerOnBridge)
				{
					if (changed)
					{
						Body.Flags = oldFlags;
						Body.Model = oldModel;
						BroadcastMessage("Daewain croaks softly as he rests in the shade under the bridge.");
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
			}
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "DaewainBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			base.Think();
		}
	}
}


