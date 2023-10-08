using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
	"&blink",
	EPrivLevel.GM,
	"Makes the specified UI Part of your target or yourself blinking.",
	"/blink <id>: type /blink for a list of possible IDs")]
public class BlinkCommand : ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		GamePlayer player = client.Player;
        bool sendBlinkPanel = false;

		// If an argument is given (an Int value is expected)
		if (args.Length > 1)
		{
			// The value that is given to us
			byte value;
			// Try to parse the Int value to byte and put the result in "value"
            if (byte.TryParse(args[1].ToLower(), out value))
            {
				// Try to find the value in ePanel Enumerator
                if (Enum.IsDefined(typeof(ePanel), value))
                {
					// Give the user some information
                    client.Out.SendMessage("Start blinking UI part: " + Enum.GetName(typeof(ePanel), value), eChatType.CT_System, eChatLoc.CL_SystemWindow);

					// If we have a target, send the blink panel to him or make our own UI blink otherwise
                    if (player.TargetObject != null && player.TargetObject is GamePlayer && (player.TargetObject as GamePlayer).Client.IsPlaying)
                        (player.TargetObject as GamePlayer).Out.SendBlinkPanel(value);
                    else
                        player.Out.SendBlinkPanel(value);

					// Send blink panel successfull
                    sendBlinkPanel = true;
                }
            }
		}

		// If an error occured, say the user what to do
		if (sendBlinkPanel == false)
		{
			Usage(client);
		}
	}


	/// <summary>
	/// Tell the user how to use this command
	/// </summary>
	private void Usage(GameClient client)
	{
		// Create a new string and add some Info to it
		String visualEffectList = "";

		visualEffectList += "You must specify a value!\nID: Name\n";

		int count = 0;

		// For each Item in ePanel, write the current ID and the Name to our string
		foreach (string panelID in Enum.GetNames(typeof(ePanel)))
		{
			visualEffectList += count + ": " + panelID + "\n";
			count++;
		}

		// Give the user some usefull output
		client.Out.SendMessage(visualEffectList, eChatType.CT_System, eChatLoc.CL_SystemWindow);
	}
}