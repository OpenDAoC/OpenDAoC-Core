using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Commands
{
	[Command("&bang", EPrivLevel.Player, "Bang on your shield", "/bang")]
	[Command("&beckon", EPrivLevel.Player, "Makes a beckoning gesture with character's hand", "/beckon")]
	[Command("&beg", EPrivLevel.Player, "Plea for items or money", "/beg")]
	[Command("&blush", EPrivLevel.Player, "Act in a shy manner", "/blush")]
	[Command("&bow", EPrivLevel.Player, "Give an honorable bow", "/bow")]
	[Command("&charge", EPrivLevel.Player, "Onward!", "/charge")]
	[Command("&cheer", EPrivLevel.Player, "Cheer with both hands in the air", "/cheer")]
	[Command("&clap", EPrivLevel.Player, "Make a clapping gesture", "/clap")]
	[Command("&cry", EPrivLevel.Player, "Sob pathetically", "/cry")]
	[Command("&curtsey", EPrivLevel.Player, "Give a low curtsey", "/curtsey")]
	[Command("&dance", EPrivLevel.Player, "Dance a little jig", "/dance")]
	[Command("&dismiss", EPrivLevel.Player, "Make a dismissing gesture", "/dismiss")]
	[Command("&flex", EPrivLevel.Player, "Makes a flexing gesture", "/flex")]
	[Command("&hug", EPrivLevel.Player, "Makes a hugging gesture", "/hug")]
	[Command("&induct", EPrivLevel.Player, "Make a ceremonial induction gesture", "/induct")]
	[Command("&kiss", EPrivLevel.Player, "Blow a kiss", "/kiss")]
	[Command("&laugh", EPrivLevel.Player, "Makes a laughing gesture", "/bang")]
	[Command("&military", EPrivLevel.Player, "A military salute", "/military")]
	[Command("&no", EPrivLevel.Player, "Shake your head", "/no")]
	[Command("&point", EPrivLevel.Player, "Points to something in front of you", "/point")]
	[Command("&ponder", EPrivLevel.Player, "For when you just want to go \"hmmmmmmm\"", "/ponder")]
	[Command("&present", EPrivLevel.Player, "To present someone with something", "/present")]
	[Command("&raise", EPrivLevel.Player, "Raise your hand, as in volunteering or getting attention", "/raise")]
	[Command("&rude", EPrivLevel.Player, "A rude gesture", "/rude")]
	[Command("&salute", EPrivLevel.Player, "Makes a stiff salute", "/salute")]
	[Command("&shrug", EPrivLevel.Player, "Shrug your shoulders", "/shrug")]
	[Command("&slap", EPrivLevel.Player, "Slap someone", "/slap")]
	[Command("&slit", EPrivLevel.Player, "Let your enemy know what you want to do to him", "/slit")]
	[Command("&surrender", EPrivLevel.Player, "I give up!", "/surrender")]
	[Command("&taunt", EPrivLevel.Player, "A very mean gesture when you really want to insult someone", "/taunt")]
	[Command("&victory", EPrivLevel.Player, "Make a victory cheer", "/victory")]
	[Command("&wave", EPrivLevel.Player, "Makes a waving gesture", "/wave")]
	[Command("&yes", EPrivLevel.Player, "Nod your head", "/yes")]
	//New
	[Command("&sweat", EPrivLevel.Player, "sweat", "/sweat")]
	[Command("&stagger", EPrivLevel.Player, "stagger", "/stagger")]
	[Command("&yawn", EPrivLevel.Player, "yawn", "/yawn")]
	[Command("&doh", EPrivLevel.Player, "doh", "/doh")]
	[Command("&confuse", EPrivLevel.Player, "Confused", "/confuse")]
	[Command("&shiver", EPrivLevel.Player, "shiver", "/shiver")]
	[Command("&rofl", EPrivLevel.Player, "rofl", "/rofl")]
	[Command("&mememe", EPrivLevel.Player, "mememe", "/mememe")]
	[Command("&worship", EPrivLevel.Player, "worship", "/worship")]
	[Command("&drink", EPrivLevel.Player, "drink", "/drink")]
	[Command("&angry", EPrivLevel.Player, "Look angrily around", "/angry")]
	[Command("&lookfar", EPrivLevel.Player, "Lets you look into the distance", "/lookfar")]
	[Command("&smile", EPrivLevel.Player, "Make a big smile", "/smile")]
	[Command("&stench", EPrivLevel.Player, "Wave away the local stench", "/stench")]
	//New
	[Command("&howl", EPrivLevel.Player, "Howl with rage", "/howl")]
    [Command("&diabolical", EPrivLevel.Player, "Sneer diabolically", "/diabolical")]
    [Command("&brandish", EPrivLevel.Player, "Brandish your weapon", "/brandish")]
    [Command("&startled", EPrivLevel.Player, "Be startled", "/startled")]
    [Command("&talk", EPrivLevel.Player, "Talk", "/talk")]
    [Command("&monty", EPrivLevel.Player, "Think Camelot is a silly place", "/monty")]
    [Command("&loco", EPrivLevel.Player, "Think this is crazy", "/loco")]
    [Command("&cower", EPrivLevel.Player, "Cower", "/cower")]
	
	public class EmoteCommand : ACommandHandler, ICommandHandler
	{
		private const ushort EMOTE_RANGE_TO_TARGET = 2048; // 2064 was out of range and 2020 in range;
		private const ushort EMOTE_RANGE_TO_OTHERS = 512; // 519 was out of range and 504 in range;

		public void OnCommand(GameClient client, string[] args)
		{
			// no emotes if dead
			if (!client.Player.IsAlive)
			{
				DisplayMessage(client, "You can't do that, you're dead!");
				return;
			}

			// no emotes in combat / mez / stun
			if (client.Player.attackComponent.AttackState || client.Player.IsMezzed || client.Player.IsStunned)
			{
				DisplayMessage(client, "You can't do that, you're busy!");
				return;
			}

			if (client.Player.IsMuted)
			{
				client.Player.Out.SendMessage("You have been muted and cannot emote!", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
				return;
			}

			if (client.Player.TargetObject != null)
			{
				// target not in range
				if( client.Player.IsWithinRadius( client.Player.TargetObject, EMOTE_RANGE_TO_TARGET ) == false )
				{
					DisplayMessage(client, "You don't see your target around here.");
					return;
				}
			}
			const string EMOTE_TICK = "Emote_Tick";
			long Tick = client.Player.TempProperties.GetProperty<long>(EMOTE_TICK);
			if (Tick > 0 && client.Player.CurrentRegion.Time - Tick  <= 0) //
			{
				client.Player.TempProperties.RemoveProperty(EMOTE_TICK);
			}
			
			long changeTime = client.Player.CurrentRegion.Time - Tick;
			if (changeTime < ServerProperties.Properties.EMOTE_DELAY && Tick > 0)
			{
				string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Emotes.Message" + Util.Random(1,4).ToString());
				client.Player.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			client.Player.TempProperties.SetProperty(EMOTE_TICK, client.Player.CurrentRegion.Time);
			EEmote emoteID;
			string[] emoteMessages;

			switch (args[0])
			{
				case "&angry":
					emoteID = EEmote.Angry;
					emoteMessages = EMOTE_MESSAGES_ANGRY;
					break;
				case "&bang":
					emoteID = EEmote.BangOnShield;
					emoteMessages = EMOTE_MESSAGES_BANG;
					break;
				case "&beckon":
					emoteID = EEmote.Beckon;
					emoteMessages = EMOTE_MESSAGES_BECKON;
					break;
				case "&beg":
					emoteID = EEmote.Beg;
					emoteMessages = EMOTE_MESSAGES_BEG;
					break;
				case "&blush":
					emoteID = EEmote.Blush;
					emoteMessages = EMOTE_MESSAGES_BLUSH;
					break;
				case "&bow":
					emoteID = EEmote.Bow;
					emoteMessages = EMOTE_MESSAGES_BOW;
					break;
				case "&charge":
					emoteID = EEmote.LetsGo;
					emoteMessages = EMOTE_MESSAGES_CHARGE;
					break;
				case "&cheer":
					emoteID = EEmote.Cheer;
					emoteMessages = EMOTE_MESSAGES_CHEER;
					break;
				case "&clap":
					emoteID = EEmote.Clap;
					emoteMessages = EMOTE_MESSAGES_CLAP;
					break;
				case "&cry":
					emoteID = EEmote.Cry;
					emoteMessages = EMOTE_MESSAGES_CRY;
					break;
				case "&curtsey":
					emoteID = EEmote.Curtsey;
					emoteMessages = EMOTE_MESSAGES_CURTSEY;
					break;
				case "&dance":
					emoteID = EEmote.Dance;
					emoteMessages = EMOTE_MESSAGES_DANCE;
					break;
				case "&dismiss":
					emoteID = EEmote.Dismiss;
					emoteMessages = EMOTE_MESSAGES_DISMISS;
					break;
				case "&flex":
					emoteID = EEmote.Flex;
					emoteMessages = EMOTE_MESSAGES_FLEX;
					break;
				case "&hug":
					emoteID = EEmote.Hug;
					emoteMessages = EMOTE_MESSAGES_HUG;
					break;
				case "&induct":
					emoteID = EEmote.Induct;
					emoteMessages = EMOTE_MESSAGES_INDUCT;
					break;
				case "&lookfar":
					emoteID = EEmote.Rider_LookFar;
					emoteMessages = EMOTE_MESSAGES_RIDER_LOOKFAR;
					break;
				case "&kiss":
					emoteID = EEmote.BlowKiss;
					emoteMessages = EMOTE_MESSAGES_KISS;
					break;
				case "&laugh":
					emoteID = EEmote.Laugh;
					emoteMessages = EMOTE_MESSAGES_LAUGH;
					break;
				case "&military":
					emoteID = EEmote.Military;
					emoteMessages = EMOTE_MESSAGES_MILITARY;
					break;
				case "&no":
					emoteID = EEmote.No;
					emoteMessages = EMOTE_MESSAGES_NO;
					break;
				case "&point":
					emoteID = EEmote.Point;
					emoteMessages = EMOTE_MESSAGES_POINT;
					break;
				case "&ponder":
					emoteID = EEmote.Ponder;
					emoteMessages = EMOTE_MESSAGES_PONDER;
					break;
				case "&present":
					emoteID = EEmote.Present;
					emoteMessages = EMOTE_MESSAGES_PRESENT;
					break;
				case "&raise":
					emoteID = EEmote.Raise;
					emoteMessages = EMOTE_MESSAGES_RAISE;
					break;
				case "&rude":
					emoteID = EEmote.Rude;
					emoteMessages = EMOTE_MESSAGES_RUDE;
					break;
				case "&salute":
					emoteID = EEmote.Salute;
					emoteMessages = EMOTE_MESSAGES_SALUTE;
					break;
				case "&shrug":
					emoteID = EEmote.Shrug;
					emoteMessages = EMOTE_MESSAGES_SHRUG;
					break;
				case "&slap":
					emoteID = EEmote.Slap;
					emoteMessages = EMOTE_MESSAGES_SLAP;
					break;
				case "&slit":
					emoteID = EEmote.Slit;
					emoteMessages = EMOTE_MESSAGES_SLIT;
					break;
				case "&smile":
					emoteID = EEmote.Smile;
					emoteMessages = EMOTE_MESSAGES_SMILE;
					break;
				case "&stench":
					emoteID = EEmote.Rider_Stench;
					emoteMessages = EMOTE_MESSAGES_RIDER_STENCH;
					break;
				case "&surrender":
					emoteID = EEmote.Surrender;
					emoteMessages = EMOTE_MESSAGES_SURRENDER;
					break;
				case "&taunt":
					emoteID = EEmote.Taunt;
					emoteMessages = EMOTE_MESSAGES_TAUNT;
					break;
				case "&victory":
					emoteID = EEmote.Victory;
					emoteMessages = EMOTE_MESSAGES_VICTORY;
					break;
				case "&wave":
					emoteID = EEmote.Wave;
					emoteMessages = EMOTE_MESSAGES_WAVE;
					break;
				case "&yes":
					emoteID = EEmote.Yes;
					emoteMessages = EMOTE_MESSAGES_YES;
					break;
				case "&sweat":
					emoteID = EEmote.Sweat;
					emoteMessages = EMOTE_MESSAGES_SWEAT;
					break;
				case "&stagger":
					emoteID = EEmote.Stagger;
					emoteMessages = EMOTE_MESSAGES_STAGGER;
					break;
				case "&yawn":
					emoteID = EEmote.Yawn;
					emoteMessages = EMOTE_MESSAGES_YAWN;
					break;
				case "&doh":
					emoteID = EEmote.Doh;
					emoteMessages = EMOTE_MESSAGES_DOH;
					break;
				case "&confuse":
					emoteID = EEmote.Confused;
					emoteMessages = EMOTE_MESSAGES_CONFUSE;
					break;
				case "&shiver":
					emoteID = EEmote.Shiver;
					emoteMessages = EMOTE_MESSAGES_SHIVER;
					break;
				case "&rofl":
					emoteID = EEmote.Rofl;
					emoteMessages = EMOTE_MESSAGES_ROFL;
					break;
				case "&mememe":
					emoteID = EEmote.Mememe;
					emoteMessages = EMOTE_MESSAGES_MEMEME;
					break;
				case "&worship":
					emoteID = EEmote.Worship;
					emoteMessages = EMOTE_MESSAGES_WORSHIP;
					break;
				case "&drink":
					emoteID = EEmote.Drink;
					emoteMessages = EMOTE_MESSAGES_DRINK;
					break;
				// new
                case "&howl":
                    emoteID = EEmote.Howl;
                    emoteMessages = EMOTE_MESSAGES_HOWL;
                    break;
                case "&diabolical":
                    emoteID = EEmote.Diabolical;
                    emoteMessages = EMOTE_MESSAGES_DIABOLICAL;
                    break;
                case "&brandish":
                    emoteID = EEmote.Brandish;
                    emoteMessages = EMOTE_MESSAGES_BRANDISH;
                    break;
                case "&startled":
                    emoteID = EEmote.Startled;
                    emoteMessages = EMOTE_MESSAGES_STARTLED;
                    break;
                case "&talk":
                    emoteID = EEmote.Talk;
                    emoteMessages = EMOTE_MESSAGES_TALK;
                    break;
                case "&monty":
                    emoteID = EEmote.Monty;
                    emoteMessages = EMOTE_MESSAGES_MONTY;
                    break;
                case "&loco":
                    emoteID = EEmote.Loco;
                    emoteMessages = EMOTE_MESSAGES_LOCO;
                    break;
                case "&cower":
                    emoteID = EEmote.Cower;
                    emoteMessages = EMOTE_MESSAGES_COWER;
                    break;
				default:
					return;
			}

			SendEmote(client.Player, client.Player.TargetObject, emoteID, emoteMessages);
		}


		// send emote animation to all visible players and format messages
		private void SendEmote(GamePlayer sourcePlayer, GameObject targetObject, EEmote emoteID, string[] emoteMessages)
		{
			string messageToSource = null;
			string messageToTarget = null;
			string messageToOthers = null;

			if (targetObject == null)
			{
				messageToSource = emoteMessages[EMOTE_NOTARGET_TO_SOURCE];
				messageToOthers = string.Format(emoteMessages[EMOTE_NOTARGET_TO_OTHERS], sourcePlayer.Name);
			}
			else
			{
				messageToSource = string.Format(emoteMessages[EMOTE_TO_SOURCE], targetObject.GetName(0, false));
				messageToOthers = string.Format(emoteMessages[EMOTE_TO_OTHERS], sourcePlayer.Name, targetObject.GetName(0, false));

				if (targetObject is GamePlayer)
					messageToTarget = string.Format(emoteMessages[EMOTE_TO_OTHERS], sourcePlayer.Name, YOU);
			}

			foreach (GamePlayer player in sourcePlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				if (!player.IsIgnoring(sourcePlayer))
					player.Out.SendEmoteAnimation(sourcePlayer, emoteID);

			SendEmoteMessages(sourcePlayer, targetObject as GamePlayer, messageToSource, messageToTarget, messageToOthers);

			return;
		}


		// send emote messages to all players in range
		private void SendEmoteMessages(GamePlayer sourcePlayer, GamePlayer targetPlayer, string messageToSource, string messageToTarget, string messageToOthers)
		{
			SendEmoteMessage(sourcePlayer, messageToSource);

			if (targetPlayer != null)
			{
				if (!targetPlayer.IsIgnoring(sourcePlayer))
					SendEmoteMessage(targetPlayer, messageToTarget);
			}

			foreach (GamePlayer player in sourcePlayer.GetPlayersInRadius(EMOTE_RANGE_TO_OTHERS))
				if (player != sourcePlayer && player != targetPlayer && !player.IsIgnoring(sourcePlayer)) // client and target gets unique messages
					SendEmoteMessage(player, messageToOthers);

			return;
		}


		// send emote chat type message to GamePlayer
		private void SendEmoteMessage(GamePlayer player, string message)
		{
			player.Out.SendMessage(message, EChatType.CT_Emote, EChatLoc.CL_SystemWindow);
		}


		private const byte EMOTE_NOTARGET_TO_SOURCE = 0; // nothing selected; send to client
		private const byte EMOTE_NOTARGET_TO_OTHERS = 1; // nothing selected; send to others
		private const byte EMOTE_TO_SOURCE = 2; // target selected; send to client; arg0 is target.name
		private const byte EMOTE_TO_OTHERS = 3; // target selected; send to others; arg0 is client.name; arg1 is target.name

		// ("{0} bangs on his shield at {1}!", Player.Name, YOU); sent to target
		private const string YOU = "you";

		private readonly string[] EMOTE_MESSAGES_BANG = {
			"You bang on your shield!",
			"{0} bangs on his shield!",
			"You bang on your shield at {0}!",
			"{0} bangs on his shield at {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_BECKON = {
			"You make a beckoning motion.",
			"{0} beckons at nothing in particular.",
			"You beckon {0}.",
			"{0} beckons {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_BEG = {
			"You beg everyone.",
			"{0} begs everyone.",
			"You beg {0}.",
			"{0} begs {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_BLUSH = {
			"You blush.",
			"{0} blushes.",
			"You blush at {0}.",
			"{0} blushes at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_BOW = {
			"You bow.",
			"{0} bows.",
			"You bow to {0}.",
			"{0} bows to {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_CHARGE = {
			"You motion everyone onward.",
			"{0} motions everyone onward.",
			"You motion {0} onward.",
			"{0} motions {1} onward.",
		};

		private readonly string[] EMOTE_MESSAGES_CHEER = {
			"You cheer wildly!",
			"{0} cheers!",
			"You cheer at {0}!",
			"{0} cheers at {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_CLAP = {
			"You give a round of applause!",
			"{0} claps!",
			"You clap at {0}!",
			"{0} claps at {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_CRY = {
			"You begin sobbing!",
			"{0} cries!",
			"You cry at {0}!",
			"{0} cries at {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_CURTSEY = {
			"You curtsey gracefully.",
			"{0} curtseys.",
			"You curtsey to {0}.",
			"{0} curtseys to {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_DANCE = {
			"You begin dancing!",
			"{0} dances!",
			"You dance with {0}!",
			"{0} dances with {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_DISMISS = {
			"You dismiss everyone.",
			"{0} dismisses everyone.",
			"You dismiss {0}.",
			"{0} dismisses {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_FLEX = {
			"You flex!",
			"{0} flexes!",
			"You flex at {0}!",
			"{0} flexes at {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_HUG = {
			"You hug yourself.",
			"{0} hugs himself.",
			"You hug {0}.",
			"{0} hugs {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_INDUCT = {
			"You induct everyone.",
			"{0} inducts everyone.",
			"You induct {0}.",
			"{0} inducts {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_KISS = {
			"You blow a kiss!",
			"{0} blows a kiss!",
			"You blow a kiss to {0}!",
			"{0} blows a kiss to {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_LAUGH = {
			"You burst into laughter!",
			"{0} laughs!",
			"You laugh at {0}!",
			"{0} laughs at {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_MILITARY = {
			"You salute.",
			"{0} salutes.",
			"You salute {0}.",
			"{0} salutes {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_NO = {
			"You shake your head no.",
			"{0} shakes his head no.",
			"You shake your head no at {0}.",
			"{0} shakes his head no at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_POINT = {
			"You point into the distance.",
			"{0} points.",
			"You point at {0}.",
			"{0} points at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_PONDER = {
			"You ponder.",
			"{0} ponders.",
			"You ponder at {0}.",
			"{0} ponders at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_PRESENT = {
			"You present.",
			"{0} presents.",
			"You present to {0}.",
			"{0} presents to {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_RAISE = {
			"You raise your hand.",
			"{0} raises his hand.",
			"You raise your hand at {0}.",
			"{0} raises his hand at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_RUDE = {
			"You make a rude gesture.",
			"{0} makes a rude gesture.",
			"You make a rude gesture at {0}.",
			"{0} makes a rude gesture at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_SALUTE = {
			"You give a firm salute.",
			"{0} gives a firm salute.",
			"You salute {0}.",
			"{0} salutes {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_SHRUG = {
			"You shrug.",
			"{0} shrugs.",
			"You shrug at {0}.",
			"{0} shrugs at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_SLAP = {
			"You make a slapping motion.",
			"{0} makes a slapping motion.",
			"You slap {0}!",
			"{0} slaps {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_SLIT = {
			"You make a throat slit motion.",
			"{0} makes a throat slit motion.",
			"You make a throat slit motion at {0}.",
			"{0} makes a throat slit motion at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_SURRENDER = {
			"You make a surrendering gesture.",
			"{0} makes a surrendering gesture.",
			"You make a surrendering gesture at {0}.",
			"{0} makes a surrendering gesture at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_TAUNT = {
			"You make a taunting gesture.",
			"{0} makes a taunting gesture.",
			"You taunt {0}.",
			"{0} taunts {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_VICTORY = {
			"You howl in victory!",
			"{0} howls in victory!",
			"You howl in victory to {0}!",
			"{0} howls in victory to {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_WAVE = {
			"You wave at everyone.",
			"{0} waves at everyone.",
			"You wave to {0}.",
			"{0} waves to {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_YES = {
			"You nod your head yes.",
			"{0} nods his head yes.",
			"You nod your head yes at {0}.",
			"{0} nods his head yes at {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_SWEAT = {
			"You break into a sweat.",
			"{0} breaks into a sweat.",
			"You break into a sweat at the sight of {0}.",
			"{0} breaks into a sweat at the sight of {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_STAGGER = {
			"You stagger.",
			"{0} staggers.",
			"You stagger towards {0}.",
			"{0} staggers towards {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_YAWN = {
			"You yawn.",
			"{0} yawns.",
			"You yawn at {0}.",
			"{0} yawns at {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_DOH = {
			"You slap your head in confusion.",
			"{0} slaps your head in confusion.",
			"You slap your head in confusion at {0}.",
			"{0} slaps his head in confusion at {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_CONFUSE = {
			"You look at yourself, clearly confused.",
			"{0} looks at you, clearly confused.",
			"You look at {0}, clearly confused.",
			"{0} looks at {1}, clearly confused.",
		};
		private readonly string[] EMOTE_MESSAGES_SHIVER = {
			"You shiver.",
			"{0} shivers.",
			"You shiver at the sight of {0}.",
			"{0} shivers at the sight of {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_ROFL = {
			"You roll on the floor laughing.",
			"{0} rolls on the floor laughing at.",
			"You roll on the floor laughing at {0}.",
			"{0} rolls on the floor laughing at {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_MEMEME = {
			"You beg to pick you.",
			"{0} begs to pick him.",
			"You beg {0} to pick you.",
			"{0} begs {1} to pick them.",
		};
		private readonly string[] EMOTE_MESSAGES_WORSHIP = {
			"You worship everything, and yet nothing.",
			"{0} worships nothing in particular.",
			"You worship {0}!",
			"{0} worships {1}!",
		};
		private readonly string[] EMOTE_MESSAGES_DRINK = {
			"You take a drink.",
			"{0} takes a drink.",
			"You toast {0} and take a drink.",
			"{0} toasts {1}!",
		};

		private readonly string[] EMOTE_MESSAGES_ANGRY = {
			"You look angrily around.",
			"{0} looks angrily.",
			"You look angrily at {0}.",
			"{0} looks angrily at {1}.",
		};

		private readonly string[] EMOTE_MESSAGES_RIDER_LOOKFAR = {
			"You look into the distance.",
			"{0} looks into the distance.",
			"You look into the distance.",
			"{0} looks into the distance.",
		};

		private readonly string[] EMOTE_MESSAGES_RIDER_STENCH = {
			"You wave away the local stench.",
			"{0} waves away the local stench.",
			"You wave away the stench of {0}.",
			"{0} waves away the stench of {1}.",
		};
		private readonly string[] EMOTE_MESSAGES_SMILE = {
			"You smile happily.",
			"{0} smiles happily.",
			"You smile at {0}.",
			"{0} smiles at {1}.",
		};
		// new
        private readonly string[] EMOTE_MESSAGES_HOWL = {
            "You howl with rage.",
            "{0} howls with rage.",
            "You howl with rage at {0}.",
            "{0} howls with rage at {1}.",
        };
        private readonly string[] EMOTE_MESSAGES_DIABOLICAL = {
            "You sneer diabolically.",
            "{0} sneer diabolically.",
            "You sneer diabolically at {0}.",
            "{0} sneer diabolically at {1}.",
        };
        private readonly string[] EMOTE_MESSAGES_BRANDISH = {
            "You brandish your weapon.",
            "{0} brandishes their weapon.",
            "You brandish your weapon at {0}.",
            "{0} brandishes their weapon at {1}.",
        };
        private readonly string[] EMOTE_MESSAGES_STARTLED = {
            "You are startled.",
            "{0} is startled.",
            "You are startled by {0}.",
            "{0} is startled by {1}.",
        };
        private readonly string[] EMOTE_MESSAGES_TALK = {
            "You start talking with the hope that someone will listen.",
            "{0} starts talking with the hope that someone will listen.",
            "You talk to {0}.",
            "{0} talks to {1}.",
        };
        private readonly string[] EMOTE_MESSAGES_MONTY = {
            "You think Camelot is a silly place.",
            "{0} thinks Camelot is a silly place.",
            "You inform {0} that Camelot is a silly place.",
            "{0} informs {0} that Camelot is a silly place.",
        };
        private readonly string[] EMOTE_MESSAGES_LOCO = {
            "You think this is crazy.",
            "{0} thinks this is crazy.",
            "You think {0} is crazy.",
            "{0} thinks {1} is crazy.",
        };
        private readonly string[] EMOTE_MESSAGES_COWER = {
            "You cower.",
            "{0} cowers.",
            "You cower before {0}.",
            "{0} cowers before {1}.",
        };
	}
}
