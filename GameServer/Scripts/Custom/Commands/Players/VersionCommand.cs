using System.Reflection;
using Core.GS.Enums;

namespace Core.GS.Commands
{
	[Command(
		"&version",
		EPrivLevel.Player,
		"Get the version of the GameServer",
		"/version")]
	public class VersionCommand : ACommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			AssemblyName an = Assembly.GetAssembly(typeof(GameServer)).GetName();
			client.Out.SendMessage("Dawn of Light " + an.Name + " Version: " + an.Version, EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
	}
}