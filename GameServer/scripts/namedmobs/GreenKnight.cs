/*
 * Author:	Kelteen
 * Date:	10.09.2021
 * This Script is for the Green Knight in Old Frontiers/RvR
 * Script is for interacting with players.
 */
using System;
using System.Reflection;
using DOL.GS.PacketHandler;
using log4net;
using DOL.Events;

namespace DOL.GS.Scripts
{


	//Green Knight NPC that speaks and
	//answers to the right click (interact) of players
	public class GreenKnight : GameNPC
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		public override bool AddToWorld()
		{

			this.Name = "Green Knight";
			this.GuildName = "";
			this.Model = 380;
			this.Size = 120;
			this.Level = 75;
			this.EquipmentTemplateID = "Green";
			this.Realm = eRealm.None;
			Flags &= eFlags.PEACE;
			this.Faction = FactionMgr.GetFactionByID(62);
			base.AddToWorld();
			return true;
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Green Knight NPC Initializing...");
		}


		//This function is the callback function that is called when
		//a player right clicks on the npc
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			//Now we turn the npc into the direction of the person it is
			//speaking to.
			TurnTo(player.X, player.Y);

			//We send a message to player and make it appear in a popup
			//window. Text inside the [brackets] is clickable in popup
			//windows and will generate a /whis text command!
			player.Out.SendMessage(
				"You are wise to speak with me " + player.CharacterClass.Name + "! My forest is a delicate beast that can easily turn against you. " +
				"Should you wake the beast within, I must then rise to [defend it].",
				eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return true;
		}

		//This function is the callback function that is called when
		//someone whispers something to this mob!
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str))
				return false;

			//If the source is no player, we return false
			if (!(source is GamePlayer))
				return false;

			//We cast our source to a GamePlayer object
			GamePlayer t = (GamePlayer)source;

			//Now we turn the npc into the direction of the person it is
			//speaking to.
			TurnTo(t.X, t.Y);

			//We test what the player whispered to the npc and
			//send a reply. The Method SendReply used here is
			//defined later in this class ... read on
			switch (str)
			{
				case "defend it":
					SendReply(t,
							  "Caution will be your guide through the dark places of Sauvage. " +
								  "Tread lightly " + t.CharacterClass.Name + "! I am ever watchful of my home!");
					break;
				case "defend":
					SendReply(t,
							  "Caution will be your guide through the dark places of Sauvage. " +
								  "Tread lightly " + t.CharacterClass.Name + "! I am ever watchful of my home!");
					break;
				default:
					break;
			}
			return true;
		}

		//This function sends some text to a player and makes it appear
		//in a popup window. We just define it here so we can use it in
		//the WhisperToMe function instead of writing the long text
		//everytime we want to send some reply!
		private void SendReply(GamePlayer target, string msg)
		{
			target.Out.SendMessage(
				msg,
				eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}
	}
}