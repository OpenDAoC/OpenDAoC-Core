// using DOL.GS;
//
// namespace Core.GS.Commands
// {
// 	[CmdAttribute(
// 		"&deal",
// 		ePrivLevel.Player,
// 		"Deal one card to a player in your group. Cards must first be prepared with /shuffle.",
// 		"/deal <name> <u/d>")]
// 	public class DealCommand : ACommandHandler, ICommandHandler
// 	{
// 		public void OnCommand(GameClient client, string[] args)
// 		{
// 			if (args.Length < 3)
// 				return;
//
//             bool up = false;
//
// 			if (args[2][0] == 'u')
// 				up = true;
// 			else if
// 				(args[2][0] == 'd') up = false;
// 			else
// 				return;
//
// 			GameClient friendClient = WorldMgr.GetClientByPlayerName(args[1], true, true);
//             CardMgr.Deal(client, friendClient, up);
// 		}
// 	}
// }

namespace Core.GS.Commands;