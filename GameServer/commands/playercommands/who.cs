using System;
using System.Collections;
using System.Text;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&who",
		ePrivLevel.Player,
		"Shows who is online",
		//help:
		//"/who  Can be modified with [playername], [class], [#] level, [location], [##] [##] level range",
		"/WHO ALL - lists all players online",
		//"/WHO NF lists all players online in New Frontiers",
		// "/WHO CSR lists all Customer Service Representatives currently online",
		// "/WHO DEV lists all Development Team Members currently online",
		// "/WHO QTA lists all Quest Team Assistants currently online",
		"/WHO <name> lists - players with names that start with <name>",
		"/WHO <guild name> - lists players with names that start with <guild name>",
		"/WHO <class> - lists players with of class <class>",
		"/WHO <location> - lists players in the <location> area",
		"/WHO <level> - lists players of level <level>",
		"/WHO <level> <level> - lists players in level range",
		"/WHO BG - lists all players leading a public BattleGroup",
		"/WHO nogroup - lists all ungrouped players",
		"/WHO hc - lists all Hardcore players",
		"/WHO solo - lists all SOLO players"
	)]
	public class WhoCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public const int MAX_LIST_SIZE = 49;
		public const string MESSAGE_LIST_TRUNCATED = "(Too many matches ({0}).  List truncated.)";
		private const string MESSAGE_NO_MATCHES = "No Matches.";
		private const string MESSAGE_NO_ARGS = "Type /WHO HELP for variations on the WHO command.";
		private const string MESSAGE_PLAYERS_ONLINE = "{0} player{1} currently online.";

		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "who") && client.Account.PrivLevel == 1)
				return;
			
			int listStart = 1;
			ArrayList filters = null;
			ArrayList clientsList = new ArrayList();
			ArrayList resultMessages = new ArrayList();

			foreach (GamePlayer otherPlayer in ClientService.GetPlayers())
			{
				if (otherPlayer.Client.Account.PrivLevel > (uint) ePrivLevel.Player && otherPlayer.IsAnonymous == false)
				{
					clientsList.Add(otherPlayer.Client);
					continue;
				}

				if (otherPlayer.Client != client
					&& client.Account.PrivLevel == (uint) ePrivLevel.Player
					&& (otherPlayer.IsAnonymous || !GameServer.ServerRules.IsSameRealm(otherPlayer, client.Player, true)))
				{
					continue;
				}

				clientsList.Add(otherPlayer.Client);
			}

			// no params
			if (args.Length == 1)
			{
				int playing = clientsList.Count;

				// including anon?
				DisplayMessage(client, string.Format(MESSAGE_PLAYERS_ONLINE, playing, playing > 1 ? "s" : ""));
				DisplayMessage(client, MESSAGE_NO_ARGS);
				return;
			}
			
			// any params passed?
			switch (args[1].ToLower())
			{
				case "all": // display all players, no filter
				{
					filters = null;
					break;
				}
				case "help": // list syntax for the who command
				{
					DisplaySyntax(client);
					return;
				}
				case "staff":
				case "gm":
				case "admin":
				{
					filters = new ArrayList(1);
					filters.Add(new GMFilter());
					break;
				}
				case "en":
				case "cz":
				case "de":
				case "es":
				case "fr":
				case "it":
				{
					filters = new ArrayList(1);
					filters.Add(new LanguageFilter(args[1].ToLower()));
					break;
				}
				case "cg":
				{
					filters = new ArrayList(1);
					filters.Add(new ChatGroupFilter());
					break;
				}
				case "bg":
				{
					filters = new ArrayList(1);
					filters.Add(new BGFilter());
					break;
				}
				case "nogroup":
				{
					filters = new ArrayList();
					filters.Add(new SoloFilter());
					break;
				}
				case "rp":
				{
					filters = new ArrayList(1);
					filters.Add(new RPFilter());
					break;
				}
				case "hc":
				case "hardcore":
				{
					filters = new ArrayList(1);
					filters.Add(new HCFilter());
					break;
				}
				case "solo":
				case "nohelp":
				{
					filters = new ArrayList(1);
					filters.Add(new NoHelpFilter());
					break;
				}
				case "frontiers":
				{
					filters = new ArrayList();
					filters.Add(new OldFrontiersFilter());
					break;
				}
				case "adv": // Filter for '/advisor' system
				{
					filters = new ArrayList();
					filters.Add(new AdvisorFilter());
					break;
				}
				default:
				{
					filters = new ArrayList();
					AddFilters(filters, args, 1);
					break;
				}
			}

			int resultCount = 0;
			foreach (GameClient clients in clientsList)
			{
				if (ApplyFilter(filters, clients.Player))
				{
					resultCount++;
					if (resultMessages.Count < MAX_LIST_SIZE && resultCount >= listStart)
					{
						resultMessages.Add(resultCount + ") " + FormatLine(clients.Player, client.Account.PrivLevel, client));
					}
				}
			}

			foreach (string str in resultMessages)
			{
				DisplayMessage(client, str);
			}

			if (resultCount == 0)
			{
				DisplayMessage(client, MESSAGE_NO_MATCHES);
			}
			else if (resultCount > MAX_LIST_SIZE)
			{
				DisplayMessage(client, string.Format(MESSAGE_LIST_TRUNCATED, resultCount));
			}

			filters = null;
		}


		// make /who line using GamePlayer
		private string FormatLine(GamePlayer player, uint PrivLevel, GameClient source)
		{

			// /setwho class | trade
			// Sets how the player wishes to be displayed on a /who inquery.
			// Class displays the character's class and level.
			// Trade displays the tradeskill type and level of the character.
			// and it is saved after char logs out


			if (player == null)
			{
				if (log.IsErrorEnabled)
					log.Error("null player in who command");
				return "???";
			}

			StringBuilder result = new StringBuilder(player.Name, 100);
			if (player.GuildName != "")
			{
				result.Append(" <");
				result.Append(player.GuildName);
				result.Append(">");
			}

			result.Append(" the Level ");
			result.Append(player.Level);
			if (player.ClassNameFlag)
			{
				result.Append(" ");
				result.Append(player.CharacterClass.Name);
			}
			else if (player.CharacterClass != null)
			{
				result.Append(" ");
				AbstractCraftingSkill skill = CraftingMgr.getSkillbyEnum(player.CraftingPrimarySkill);
				result.Append(player.CraftTitle.GetValue(source.Player, player));
			}
			else
			{
				if (log.IsErrorEnabled)
					log.Error("no character class spec in who commandhandler for player " + player.Name);
			}

			if (player.CurrentZone != null && GameServer.Instance.Configuration.ServerType != EGameServerType.GST_PvP)
			{
				// If '/who' source is a Player and target is plvl 3, do not return zone description (only return for Admins if Admin is source)
				if (source.Account.PrivLevel == (uint)ePrivLevel.Player && player.Client.Account.PrivLevel == (uint)ePrivLevel.Player || source.Account.PrivLevel == (uint)ePrivLevel.Admin)
				{
					result.Append(" in ");
					// Counter-espionage behavior: Change zone description to "Frontiers" if source is a Player and target(s) located in OF (RVR-enabled zone in classic Alb/Hib/Mid region)
					if (source.Account.PrivLevel == (uint)ePrivLevel.Player && player.CurrentZone.IsRvR && player.CurrentRegion.ID is 1 or 100 or 200)
					{
						result.Append("the Frontiers");
					}
					// If target player(s) are not in RvR-enabled zones in classic region, return zone name/description
					else
					{
						result.Append(player.CurrentZone.Description);	
					}
				}
			}
			else
			{
				if (log.IsErrorEnabled && player.Client.Account.PrivLevel != (uint)ePrivLevel.Admin)
					log.Error("no currentzone in who commandhandler for player " + player.Name);
			}
			ChatGroup mychatgroup = player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
			if (mychatgroup != null && (mychatgroup.Members.Contains(player) || mychatgroup.IsPublic && (bool)mychatgroup.Members[player] == true))
			{
				result.Append(" [CG]");
			}
			BattleGroup mybattlegroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
			if (mybattlegroup != null && (mybattlegroup.Members.Contains(player) || mybattlegroup.IsPublic && (bool)mybattlegroup.Members[player] == true))
			{
				result.Append(" [BG]");
			}
			if (player.IsAnonymous)
			{
				result.Append(" <ANON>");
			}
			if (player.TempProperties.GetProperty<string>(GamePlayer.AFK_MESSAGE) != null)
			{
				result.Append(" <AFK>");
			}
			if (player.Advisor)
			{
				result.Append(" <ADV>");
			}
			if (player.HCFlag)
			{
				result.Append(" <HC>");
			}
			if (player.NoHelp)
			{
				result.Append(" <SOLO>");
			}
			if(player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
			{
				result.Append(" <GM>");
			}
			if(player.Client.Account.PrivLevel == (uint)ePrivLevel.Admin)
			{
				result.Append(" <Admin>");
			}
			if (ServerProperties.Properties.ALLOW_CHANGE_LANGUAGE)
			{
				result.Append(" <" + player.Client.Account.Language + ">");
			}

			return result.ToString();
		}

		private void AddFilters(ArrayList filters, string[] args, int skip)
		{
			for (int i = skip; i < args.Length; i++)
			{
				if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvP)
					filters.Add(new StringFilter(args[i]));
				else
				{
					try
					{
						int currentNum = (int) System.Convert.ToUInt32(args[i]);
						int nextNum = -1;
						try
						{
							nextNum = (int) System.Convert.ToUInt32(args[i + 1]);
						}
						catch
						{
						}

						if (nextNum != -1)
						{
							filters.Add(new LevelRangeFilter(currentNum, nextNum));
							i++;
						}
						else
						{
							filters.Add(new LevelFilter(currentNum));
						}
					}
					catch
					{
						filters.Add(new StringFilter(args[i]));
					}
				}
			}
		}


		private bool ApplyFilter(ArrayList filters, GamePlayer player)
		{
			if (filters == null)
				return true;
			foreach (IWhoFilter filter in filters)
			{
				if (!filter.ApplyFilter(player))
					return false;
			}
			return true;
		}


		//Filters

		private class StringFilter : IWhoFilter
		{
			private string m_filterString;

			public StringFilter(string str)
			{
				m_filterString = str.ToLower().Trim();
			}

			public bool ApplyFilter(GamePlayer player)
			{
				if (player.Name.ToLower().StartsWith(m_filterString))
					return true;
				if (player.GuildName.ToLower().StartsWith(m_filterString))
					return true;
				if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvP)
					return false;
				if (player.CharacterClass.Name.ToLower().StartsWith(m_filterString))
					return true;
				if (player.CurrentZone != null && player.CurrentZone.Description.ToLower().Contains(m_filterString) && !player.CurrentZone.IsOF)
					return true;
				return false;
			}
		}

		private class LevelRangeFilter : IWhoFilter
		{
			private int m_minLevel;
			private int m_maxLevel;

			public LevelRangeFilter(int minLevel, int maxLevel)
			{
				m_minLevel = Math.Min(minLevel, maxLevel);
				m_maxLevel = Math.Max(minLevel, maxLevel);
			}

			public bool ApplyFilter(GamePlayer player)
			{
				if (player.Level >= m_minLevel && player.Level <= m_maxLevel)
					return true;
				return false;
			}
		}

		private class LevelFilter : IWhoFilter
		{
			private int m_level;

			public LevelFilter(int level)
			{
				m_level = level;
			}

			public bool ApplyFilter(GamePlayer player)
			{
				return player.Level == m_level;
			}
		}

		private class GMFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				if(!player.IsAnonymous && player.Client.Account.PrivLevel > (uint)ePrivLevel.Player)
					return true;
				return false;
			}
		}

		private class LanguageFilter : IWhoFilter
		{
			private string m_str;
			public bool ApplyFilter(GamePlayer player)
			{
				if (!player.IsAnonymous && player.Client.Account.Language.ToLower() == m_str)
					return true;
				return false;
			}
			
			public LanguageFilter(string language)
			{
				m_str = language;
			}
		}

		private class ChatGroupFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				ChatGroup cg = player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);
				//no chatgroup found
				if (cg == null)
					return false;

				//always show your own cg
				//TODO

				//player is a cg leader, and the cg is public
				if ((bool)cg.Members[player] == true && cg.IsPublic)
					return true;

				return false;
			}
		}

		private class OldFrontiersFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				if (player.Client.Account.PrivLevel == (uint)ePrivLevel.Admin && player.CurrentZone.IsRvR)
					return false;
				if (player.Client.Account.PrivLevel < (uint)ePrivLevel.Admin && player.CurrentZone.IsRvR && player.CurrentRegion.ID is 1 or 100 or 200)
					return true;
				return false;
			}
		}

		private class RPFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				return player.RPFlag;
			}
		}
		
		private class AdvisorFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				return player.Advisor;
			}
		}
		
		private class HCFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				return player.HCFlag;
			}
		}
		
		private class NoHelpFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				return player.NoHelp;
			}
		}

		private class SoloFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				if (player.Group == null)
				{
					return true;
				}
				return false;
			}
		}
		
		private class BGFilter : IWhoFilter
		{
			public bool ApplyFilter(GamePlayer player)
			{
				BattleGroup bg = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
				//no battlegroup found
				if (bg == null)
					return false;

				//always show your own bg
				//TODO

				//player is a bg leader, and the bg is public
				if (bg.Leader == player && bg.IsPublic)
					return true;
				
				return false;
			}
		}

		private interface IWhoFilter
		{
			bool ApplyFilter(GamePlayer player);
		}
	}
}
