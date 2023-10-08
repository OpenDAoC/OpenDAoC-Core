using System;

namespace DOL.GS.Commands;

[Command("&summon", EPrivLevel.Player,"Summon horse","/summon")]
public class SummonHorseCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player == null)
			return;
		try
		{
			if (args.Length > 1 && Convert.ToInt16(args[1]) == 0)
				client.Player.IsOnHorse = false;
		}
		catch
		{
			DisplayMessage(client, "Incorrect format of the command");
		}
		finally
		{
			if (client.Player.Inventory.GetItem(eInventorySlot.Horse) != null)
				client.Player.UseSlot(eInventorySlot.Horse, eUseType.clic);
		}
	}
}