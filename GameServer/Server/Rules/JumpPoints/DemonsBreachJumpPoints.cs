using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Server;

public class NergalsBreachJumpPoint : IJumpPointHandler
{
	public bool IsAllowedToJump(DbZonePoint targetPoint, GamePlayer player)
	{
		if(player.Client.Account.PrivLevel > 1)
        {
            return true;
        }
        if(player.Level < 5)
		{
			return true;
		}
        player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DemonsBreachJumpPoint.Requirements"), EChatType.CT_System, EChatLoc.CL_ChatWindow);
		return false;
	}
	
}

public class BalbansBreachJumpPoint : IJumpPointHandler
{
	public bool IsAllowedToJump(DbZonePoint targetPoint, GamePlayer player)
	{
        if (player.Client.Account.PrivLevel > 1)
        {
            return true;
        }
        if (player.Level < 10 && player.Level > 4)
		{
			return true;
		}
        player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DemonsBreachJumpPoint.Requirements"), EChatType.CT_System, EChatLoc.CL_ChatWindow);
		return false;
	}
}