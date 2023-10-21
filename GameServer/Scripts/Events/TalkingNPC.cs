using System;
using System.Reflection;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using log4net;

namespace Core.GS.GameEvents
{
	//First, declare our Event and have it implement the IGameEvent interface
	public class TalkingNpcEvent
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		//For our event we need a special npc that
		//answers to the right click (interact) of players
		//and also answers to talk.
		public class TalkingNPC : GameNpc
		{
			public TalkingNPC() : base()
			{
				//First, we set the position of this
				//npc in the constructor. You can set
				//the npc position, model etc. in the
				//StartEvent() method too if you want.
				X = 505499;
				Y = 437679;
				Z = 0;
				Heading = 0x0;
				Name = "The talking NPC";
				GuildName = "Rightclick me";
				Model = 5;
				Size = 50;
				Level = 10;
				Realm = ERealm.Albion;
				CurrentRegionID = 1;
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
				//windows and will generate a &whis text command!
				player.Out.SendMessage(
					"Hello " + player.Name + " do you want to have a little [chat]?",
					EChatType.CT_System, EChatLoc.CL_PopupWindow);
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
				GamePlayer t = (GamePlayer) source;

				//Now we turn the npc into the direction of the person it is
				//speaking to.
				TurnTo(t.X, t.Y);

				//We test what the player whispered to the npc and
				//send a reply. The Method SendReply used here is
				//defined later in this class ... read on
				switch (str)
				{
					case "chat":
						SendReply(t,
						          "Oh, that's nice!\n I can tell you more about " +
						          	"[DOL]. [DOL] is a really cool project and their " +
						          	"[scripting] is really powerful!");
						break;
					case "scripting":
						SendReply(t,
						          "Yeah! I myself am just a [script] actually! " +
						          	"Isn't that cool? It is so [easy] to use!");
						break;
					case "DOL":
						SendReply(t,
						          "DOL? Dawn Of Light, yes yes, this server is running " +
						          	"Dawn Of Light! I am a [script] on this server, the " +
						          	"scripting language is really powerful and [easy] to use!");
						break;
					case "script":
						SendReply(t,
						          "Yes, I am a script. If you look into the /scripts/cs/gameevents directory " +
						          	"of your DOL version, you should find a script called " +
						          	"\"TalkingNPC.cs\" Yes, this is me actually! Want to [chat] some more?");
						break;
					case "easy":
						SendReply(t,
						          "Scripting is easy to use. Take a look at my [script] to " +
						          	"get a clue how easy it is!");
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
					EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}
		}

		private static TalkingNPC m_npc;

		//This function is implemented from the IGameEvent
		//interface and is called on serverstart when the
		//events need to be started
		[ScriptLoadedEvent]
		public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_EXAMPLES)
				return;
			//Here we create an instance of our talking NPC
			m_npc = new TalkingNPC();
			//And add it to the world (the position and all
			//other relevant data is defined in the constructor
			//of the NPC
			bool good = m_npc.AddToWorld();
			if (log.IsInfoEnabled)
				log.Info("TalkingNPCEvent initialized");
		}

		//This function is implemented from the IGameEvent
		//interface and is called on when we want to stop 
		//an event
		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_EXAMPLES)
				return;
			//To stop this event, we simply delete
			//(remove from world completly) the npc
			if (m_npc != null)
				m_npc.Delete();
		}
	}
}