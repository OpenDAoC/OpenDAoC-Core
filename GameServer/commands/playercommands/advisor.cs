/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using DOL.Language;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&advisor",
		ePrivLevel.Player,
		// Displays next to the command when '/cmd' is entered
		"Flags your character as a class or tradeskill Advisor (<ADV>) for new players' questions.",
		// Syntax: '/advisor' - Flags your character as an Advisor (<ADV>) to indicate that you are willing to answer new players' questions.
		// '/advisor' - Flags your character as an Advisor (<ADV>) to indicate that you are willing to answer new players' questions.
		"PLCommands.Advisor.Syntax.Advisor",
		// Message: '/advisor <advisorName> <message>' - Directly messages an Advisor with your question.
		"PLCommands.Advice.Syntax.SendAdvisor")]
	public class AdvisorCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			// If the player has had the `/mute` GM command used on them
			if (client.Player.IsMuted)
			{
				// Message: "You have been muted by Atlas staff and are not allowed to use this command."
				ChatUtil.SendGMMessage(client, "GMCommands.Mute.Err.NoUseCommand", null);
				return;
			}
			
			var craftingSkill = client.Player.GetCraftingSkillValue(client.Player.CraftingPrimarySkill) >= 1000; // Identify primary crafting skill
			var played = client.Player.PlayedTimeSinceLevel / 60 / 60; // Sets time played since last level
			var totalPlayed = client.Player.PlayedTime / 60 / 60; // Time played total for character
			
			// Anti-spamming measure
			if (IsSpammingCommand(client.Player, "advisor"))
				return;
			
			switch (args.Length)
			{
				// Syntax: '/advisor'
				case 1:
				{
					// Flag is off by default
					client.Player.Advisor = !client.Player.Advisor;

					// Player can turn on Advisor flag if:
					// Character is level 50 and has played 15+ hours at level
					// OR
					// Character has 1000+ in a primary tradeskill, '/setwho craft' active, and played 15+ hours total
					if (client.Player.Level == 50 && played >= 15 || craftingSkill && totalPlayed >= 15 && !client.Player.ClassNameFlag || client.Account.PrivLevel > (uint) ePrivLevel.Player)
					{
						// Enable the advisor flag
						if (client.Player.Advisor)
						{
							// Message: "Your Advisor flag (<ADV>) has been turned on. The Advisor system is run and used by players at their own risk. We ask that only experienced players interested in helping new players and answering basic questions use this feature. To disable this flag off at any time, simply type '/advisor' again."
							ChatUtil.SendErrorMessage(client, "PLCommands.Advisor.Msg.OnFlag", null);
						}
						else
							// Message: "Your Advisor flag has been turned off."
							ChatUtil.SendErrorMessage(client, "PLCommands.Advisor.Msg.OffFlag", null);
						return;
					}
					client.Player.Advisor = false;
					// If character doesn't meet requirements
					// Message: "You do not yet meet the requirements to become an Advisor. Class Advisors must have at least 15 hours of played time while at level 50. Tradeskill Advisors must have Legendary status in a primary tradeskill, '/setwho craft' enabled, and a minimum of 15 total hours played."
					ChatUtil.SendSystemMessage(client, "PLCommands.Advisor.Err.MinimumReqs", null);
					return;
				}
				// Syntax: '/advisor <advisorName>'
				case 2:
				{
					// Message: "You must include a message to communicate with this Advisor! Use '/advisor <advisorName> <message>' to communicate with an Advisor."
					ChatUtil.SendSystemMessage(client, "PLCommands.Advisor.Err.NoMessage", null);
					return;
				}
				// Syntax: '/advisor <advisorName> <message>'
				case >= 3:
				{
					var advisorName = args[1];
					var name = !string.IsNullOrWhiteSpace(advisorName) && char.IsLower(advisorName, 0) ? advisorName.Replace(advisorName[0],char.ToUpper(advisorName[0])) : advisorName; // If first character in args[1] is lowercase, replace with uppercase character
                    var message = string.Join(" ", args, 2, args.Length - 2); // Separate message from other args
                    var result = 3; // Result for name search with args[1]
                    var advisorClient = WorldMgr.GuessClientByPlayerNameAndRealm(name, 0, false, out result); // Figure out advisor name
                    
                    // Players cannot communicate with members of another realm
                    if (advisorClient != null && !GameServer.ServerRules.IsAllowedToUnderstand(client.Player, advisorClient.Player))
                    {
                    	advisorClient = null;
                    }
    
                    // If the player is not online, anonymous, or is a member of another realm
                    if (advisorClient == null || advisorClient.Player.IsAnonymous &&
                        advisorClient.Account.PrivLevel == (uint) ePrivLevel.Player && client.Account.PrivLevel == (uint) ePrivLevel.Player)
                    {
	                    // Message: "{0} is not in the game, or is a member of another realm."
                        ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
                    	return;
                    }
                    
                    // If the player is not online, anonymous, or is a member of another realm
                    if (!advisorClient.Player.Advisor)
                    {
	                    // Message: "{0} is not an Advisor!"
	                    ChatUtil.SendSystemMessage(client, "Social.SendAdvice.Err.NotAdvisor", name);
	                    return;
                    }
                    
                    // Atlas staff still receive messages while `/anon`, but the player doesn't see that it delivered. This is intended as a protection against players spamming Admins/GMs.
                    if (advisorClient.Player != null && advisorClient.Player.IsAnonymous &&
                        advisorClient.Account.PrivLevel > (uint) ePrivLevel.Player)
                    {
	                    if (client.Account.PrivLevel == (uint) ePrivLevel.Player)
                    	{
	                        // Message: "{0} is not in the game, or is a member of another realm."
	                        ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
	                        // Message: "{0} tried to send an advice message: {1}"
	                        ChatUtil.SendSendMessage(advisorClient.Player, "Social.SendAdvisor.Target.GMAnon", message);
                        }
                        return;
                    }

                    switch (result)
                    {
	                    case 1: // No name found
	                    {
		                    // Message: "{0} is not in the game, or is a member of another realm."
		                    ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
		                    return;
	                    }
	                    case 2: // Player name not unique (partial entry is shared between multiple characters).
	                    case 3: // Exact player name match
	                    {
		                    // If you've specified yourself
		                    if (advisorClient == client)
		                    {
			                    // Message: "You can't message yourself!"
			                    ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.CantMsgYourself", null);
		                    }
		                    else
		                    {
			                    // Message: [ADVICE] {0} sends, "{1}"
			                    ChatUtil.SendSendMessage(advisorClient.Player, "Social.SendAdvice.Msg.Sends",
				                    client.Player.Name, message);
			                    // Message: You send, "{0}" to {1} [ADVISOR].
			                    ChatUtil.SendSendMessage(client.Player, "Social.SendAdvice.Msg.YouSendTo", message,
				                    advisorClient.Player.Name);
		                    }

		                    return;
						}
	                    case 4: // Guessed name
                        {
	                        // Message: "{0} is not in the game, or is a member of another realm."
	                        ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
	                        return;
                        }
                    }
					return;
				}
			}

			
		}
	}
}