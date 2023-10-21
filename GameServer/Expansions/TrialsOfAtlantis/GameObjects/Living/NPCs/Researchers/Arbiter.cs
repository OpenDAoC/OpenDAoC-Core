using System;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS
{
    public class Arbiter : Researcher
    {
        public Arbiter()
            : base() { }

        /// <summary>
        /// Address the arbiter.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) 
                return false;

			String realm = GlobalConstants.RealmToName((ERealm)Realm);

			SayTo(player, EChatLoc.CL_PopupWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, 
				String.Format("{0}.Arbiter.Interact.Welcome", realm), player.PlayerClass.Name));

            // TODO: This appears to be level-dependent. Get the proper message
            // for all the other cases (high enough level when starting the trials
            // high enough level and trials already started).

			SayTo(player, EChatLoc.CL_PopupWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, 
				String.Format("{0}.Arbiter.Interact.BeginTrials", realm), player.Name));
            return true;
        }

        /// <summary>
		/// Talk to the arbiter.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="text"></param>
		/// <returns>True, if string needs further processing.</returns>
        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text)) return false;
            GamePlayer player = source as GamePlayer;
			String realm = GlobalConstants.RealmToName((ERealm)Realm);
			String lowerCase = text.ToLower();

			if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Case1", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text1", realm), player.PlayerClass.Name));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Case2", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text2", realm)));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
			   String.Format("{0}.Arbiter.WhisperReceive.Case3", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text3", realm)));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
			   String.Format("{0}.Arbiter.WhisperReceive.Case4", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text4", realm), player.PlayerClass.Name));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
			   String.Format("{0}.Arbiter.WhisperReceive.Case5", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text5", realm)));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
			   String.Format("{0}.Arbiter.WhisperReceive.Case6", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text6", realm), player.PlayerClass.Name));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
			   String.Format("{0}.Arbiter.WhisperReceive.Case7", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text7", realm), player.PlayerClass.Name));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
			   String.Format("{0}.Arbiter.WhisperReceive.Case8", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text8", realm), player.PlayerClass.Name));
				return false;
			}
			else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language,
				String.Format("{0}.Arbiter.WhisperReceive.Case9", realm)))
			{
				SayTo(player, EChatLoc.CL_PopupWindow,
					LanguageMgr.GetTranslation(player.Client.Account.Language,
					String.Format("{0}.Arbiter.WhisperReceive.Text9", realm), player.PlayerClass.Name));
				return false;
			}

            return true;
        }
    }
}
