using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.World;

namespace Core.GS.Commands;

[Command(
	"&addbind",
	EPrivLevel.GM,
	"GMCommands.AddBind.Description",
	"GMCommands.AddBind.Usage")]
public class AddBindCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		ushort bindRadius = 750;
		if (args.Length >= 2)
		{
			try
			{
				bindRadius = UInt16.Parse(args[1]);
			}
			catch (Exception e)
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Error", e.Message));
				return;
			}
		}
		DbBindPoint bp = new DbBindPoint();
		bp.X = client.Player.X;
		bp.Y = client.Player.Y;
		bp.Z = client.Player.Z;
		bp.Region = client.Player.CurrentRegionID;
		bp.Radius = bindRadius;
		GameServer.Database.AddObject(bp);
		client.Player.CurrentRegion.AddArea(new Area.BindArea("bind point", bp));
		DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.AddBind.BindPointAdded", bp.X, bp.Y, bp.Z, bp.Radius, bp.Region));
	}
}