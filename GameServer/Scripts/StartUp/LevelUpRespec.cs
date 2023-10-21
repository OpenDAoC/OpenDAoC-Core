using System;
using System.Reflection;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using log4net;

namespace Core.GS.GameEvents
{
	/// <summary>
	/// Class Handling Respec Granting on player Level Up.
	/// </summary>
	public static class LevelUpRespec
	{		
		/// <summary>
		/// Declare a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>
		/// What levels did we allow a DOL respec ? serialized
		/// </summary>
		[ServerProperty("startup", "give_dol_respec_at_level", "What levels does we give a DOL respec ? separated by a semi-colon or a range with a dash (ie 1-5;7;9)", "0")]
		public static string GIVE_DOL_RESPEC_AT_LEVEL;
		
		[ScriptLoadedEvent]
		public static void OnScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.LevelUp, new CoreEventHandler(OnLevelUp));
			
			if (log.IsInfoEnabled)
				log.Info("Level Up Respec Gift initialized");
		}

		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(GamePlayerEvent.LevelUp, new CoreEventHandler(OnLevelUp));
		}
		
		/// <summary>
		/// Level Up Event for Triggering Respec Gifts
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void OnLevelUp(CoreEvent e, object sender, EventArgs args)
		{
			var player = sender as GamePlayer;
			
			if (player == null)
				return;

			// Graveen: give a DOL respec on the GIVE_DOL_RESPEC_ON_LEVELS levels
			foreach (string str in Util.SplitCSV(GIVE_DOL_RESPEC_AT_LEVEL, true))
			{
				byte level_respec = 0;
				
				if(!byte.TryParse(str, out level_respec))
					level_respec = 0;

				if (player.Level == level_respec)
				{
					int oldAmount = player.RespecAmountDOL;
                    player.RespecAmountDOL++;

                    if (oldAmount != player.RespecAmountDOL)
                    {
                        player.Out.SendMessage(string.Format("As you reached level {0}, you are awarded a DOL (full) respec!", player.Level), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                    }
                }
			}
			
			// Fixed Level Respecs
			switch (player.Level)
			{
					// full respec on level 5 since 1.70
				case 5:
					//player.RespecAmountAllSkill++;
					//player.IsLevelRespecUsed = false;
					break;
				case 6:
					//if (player.IsLevelRespecUsed) break;
					//player.RespecAmountAllSkill--;
					break;

					// single line respec
				case 20:
				case 40:
					{
						//player.RespecAmountSingleSkill++; // Give character their free respecs at 20 and 40
						//player.IsLevelRespecUsed = false;
						break;
					}
				case 21:
				case 41:
					{
						//if (player.IsLevelRespecUsed) break;
						//player.RespecAmountSingleSkill--; // Remove free respecs if it wasn't used
						break;
					}
                case 50:
                    {
                        player.RespecAmountAllSkill = 1;
                        player.RespecAmountRealmSkill = 1;
                        break;
                    }
			}
		}
	}
}
