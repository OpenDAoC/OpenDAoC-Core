using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS;

// This class has to be completed and may be inherited for scripting purpose (like quests)
public class KingNPC : GameNpc
{
	private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public KingNPC()
		: base()
	{
	}

	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;

		TurnTo(player, 5000);
		if (!player.Champion && player.Level == 50)
		{
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "KingNPC.WhisperReceive.AskForChampion"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
		}

		if (player.Champion)
		{
			bool cllevel = false;

			while (player.ChampionLevel < player.ChampionMaxLevel && player.ChampionExperience >= player.ChampionExperienceForNextLevel)
			{
				player.ChampionLevelUp();
				cllevel = true;
			}

			if ( cllevel )
			{
				player.Out.SendMessage( "You reached champion level " + player.ChampionLevel + "!", EChatType.CT_System, EChatLoc.CL_PopupWindow );
			}

			if (player.ChampionLevel >= 5)
			{
				player.Out.SendMessage("I can [respecialize] your champion skills if you so desire.", EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}

		}

		return true;
	}

	public override bool WhisperReceive(GameLiving source, string str)
	{
		if (!base.WhisperReceive(source, str))
			return false;

		GamePlayer player = source as GamePlayer;
		if (player == null) return false;

		if (str == "Champions" && player.Level == 50)
		{
			if (player.Champion == true)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "KingNPC.WhisperReceive.AlreadyChampion"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
				return false;
			}

			player.RemoveChampionLevels();
			player.Champion = true;
			player.Out.SendUpdatePlayer();
			player.SaveIntoDatabase();
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "KingNPC.WhisperReceive.IsNowChampion"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
			return true;
		}

		if (str.ToLower() == "respecialize" && player.Champion && player.ChampionLevel >= 5)
		{
			player.RespecChampionSkills();
			player.SaveIntoDatabase();
			player.Out.SendMessage("I have reset your champion skills!", EChatType.CT_Important, EChatLoc.CL_PopupWindow);
		}

		return true;
	}
}