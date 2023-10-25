using Core.GS.Enums;
using Core.GS.Expansions.Foundations;

namespace Core.GS.Commands;

/// <summary>
/// Command handler to remove House player
/// </summary>
[Command(
	"&removehouse",
	EPrivLevel.GM,
	"Remove House or you are", "/removehouse")]
public class RemoveHouseCommand : ACommandHandler, ICommandHandler
{
	/// <summary>
	/// Method to handle the command from the client
	/// </summary>
	/// <param name="client"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public void OnCommand(GameClient client, string[] args)
	{
		HouseMgr.RemoveHouse(client.Player.CurrentHouse);
	}
}