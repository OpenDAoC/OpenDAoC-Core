/*
 *
 * ATLAS
 *
 */
using System;
using DOL.GS.PacketHandler;
using DOL.Database;

namespace DOL.GS.Commands
{
	[CmdAttribute("&offlinejump",
		ePrivLevel.GM,
		"GMCommands.Offlinejump.Description",
		"GMCommands.Offlinejump.Information"
	)]
	public class OfflinejumpCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			try
			{
				if (args.Length == 4 && args[2] == "to" && args[3] == "jail")
				{
					DOLCharacters character;
					character = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(args[1]));
					if (character == null)
					{
						client.Out.SendMessage("Character " + args[1] + " not found", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
						return;
					}
					character.Xpos = 47411;
					character.Ypos= 48694;
					character.Zpos = 25000;
					character.Region = 249;
					character.Direction = 5;
					BindCharacter(character);
					GameServer.Database.SaveObject(character);
					client.Out.SendMessage("Character " + args[1].ToUpperInvariant() + " has been moved to Jail", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				}
				else
				{
					DisplaySyntax(client);
				}
			}
			catch (Exception ex)
			{
				DisplayMessage(client, ex.Message);
			}
		}
		
		public static void BindCharacter(DOLCharacters ch)
		{
			ch.BindRegion = ch.Region;
			ch.BindHeading = ch.Direction;
			ch.BindXpos = ch.Xpos;
			ch.BindYpos = ch.Ypos;
			ch.BindZpos = ch.Zpos;
		}
	}
}