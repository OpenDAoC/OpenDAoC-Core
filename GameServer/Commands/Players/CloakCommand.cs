using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command("&cloak", //command to handle
	EPrivLevel.Player, //minimum privelege level
   "Show / hide your cloak.", //command description
   "Usage: /cloak [on|off].", //usage
   "Example: \"/cloak off\" to hide your cloak")] 
public class CloakCommand : ACommandHandler, ICommandHandler
{
	/* version 1.98 :
	 * [19:37:34] Usage: /cloak [on|off].
	 * [19:37:34] Example: "/cloak off" to hide your cloak
	 * [19:37:36] Your cloak will no longer be hidden from view.
	 * [19:37:39] Your cloak will now be hidden from view.
	 * */
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "cloak"))
			return;

		if (args.Length != 2)
		{
			DisplaySyntax(client);
			return;
		}
		string onOff = args[1].ToLower();
		if (onOff == "on")
		{
			if(client.Player.IsCloakInvisible)
			{
				client.Player.IsCloakInvisible = false;
				//client.Out.SendMessage("Your cloak will no longer be hidden from view.", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow);
				return;
			}
			else
			{
				client.Out.SendMessage("Your cloak is already visible.", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
				return;
			}
		}
		
		if (onOff == "off")
		{
			if (client.Player.IsCloakInvisible)
			{
				client.Out.SendMessage("Your cloak is already invisible.", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
				return;
			}
			else
			{
				client.Player.IsCloakInvisible = true;
				//client.Out.SendMessage("Your cloak will now be hidden from view.", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow);
				return;
			}
		}
		DisplaySyntax(client);
	}
}