using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS.Commands;

[Command(
	"&vault",
	EPrivLevel.Player,
	"Open the player's inventory.")]
public class VaultCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if ((ServerProperty.ALLOW_VAULT_COMMAND || client.Account.PrivLevel > 1)
			&& client.Player is GamePlayer player && player.Inventory is IGameInventory inventory)
				player.Out.SendInventoryItemsUpdate(EInventoryWindowType.PlayerVault, inventory.AllItems);
	}
}