using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Housing;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.GS.ServerProperties;
using DOL.Language;
using ECS.Debug;
using log4net;

namespace DOL.GS.ServerRules
{
	public abstract class AbstractServerRules : IServerRules
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// This is called after the rules are created to do any event binding or other tasks
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public virtual void Initialize()
		{
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(OnGameEntered));
			GameEventMgr.AddHandler(GamePlayerEvent.RegionChanged, new DOLEventHandler(OnRegionChanged));
			GameEventMgr.AddHandler(GamePlayerEvent.Released, new DOLEventHandler(OnReleased));
			m_invExpiredCallback = new GamePlayer.InvulnerabilityExpiredCallback(ImmunityExpiredCallback);
		}

		/// <summary>
		/// Allows or denies a client from connecting to the server ...
		/// NOTE: The client has not been fully initialized when this method is called.
		/// For example, no account or character data has been loaded yet.
		/// </summary>
		/// <param name="client">The client that sent the login request</param>
		/// <param name="username">The username of the client wanting to connect</param>
		/// <returns>true if connection allowed, false if connection should be terminated</returns>
		/// <remarks>You can only send ONE packet to the client and this is the
		/// LoginDenied packet before returning false. Trying to send any other packet
		/// might result in unexpected behaviour on server and client!</remarks>
		public virtual bool IsAllowedToConnect(GameClient client, string username)
		{
			if (!client.Socket.Connected)
				return false;

			// Ban account
			IList<DbBans> objs;
			objs = DOLDB<DbBans>.SelectObjects(DB.Column("Type").IsEqualTo("A").Or(DB.Column("Type").IsEqualTo("B")).And(DB.Column("Account").IsEqualTo(username)));
			if (objs.Count > 0)
			{
				client.Out.SendLoginDenied(eLoginError.AccountIsBannedFromThisServerType);
				log.Debug("IsAllowedToConnect deny access to username " + username);
				client.IsConnected = false;
				return false;
			}

			// Ban IP Address or range (example: 5.5.5.%)
			string accip = client.TcpEndpointAddress;
			objs = DOLDB<DbBans>.SelectObjects(DB.Column("Type").IsEqualTo("I").Or(DB.Column("Type").IsEqualTo("B")).And(DB.Column("Ip").IsLike(accip)));
			if (objs.Count > 0)
			{
				client.Out.SendLoginDenied(eLoginError.AccountIsBannedFromThisServerType);
				log.Debug("IsAllowedToConnect deny access to IP " + accip);
				client.IsConnected = false;
				return false;
			}

			GameClient.eClientVersion min = (GameClient.eClientVersion)Properties.CLIENT_VERSION_MIN;
			if (min != GameClient.eClientVersion.VersionNotChecked && client.Version < min)
			{
				client.Out.SendLoginDenied(eLoginError.ClientVersionTooLow);
				log.Debug("IsAllowedToConnect deny access to client version (too low) " + client.Version);
				client.IsConnected = false;
				return false;
			}

			GameClient.eClientVersion max = (GameClient.eClientVersion)Properties.CLIENT_VERSION_MAX;
			if (max != GameClient.eClientVersion.VersionNotChecked && client.Version > max)
			{
				client.Out.SendLoginDenied(eLoginError.NotAuthorizedToUseExpansionVersion);
				log.Debug("IsAllowedToConnect deny access to client version (too high) " + client.Version);
				client.IsConnected = false;
				return false;
			}

			if (Properties.CLIENT_TYPE_MAX > -1)
			{
				GameClient.eClientType type = (GameClient.eClientType)Properties.CLIENT_TYPE_MAX;
				if ((int)client.ClientType > (int)type)
				{
					client.Out.SendLoginDenied(eLoginError.ExpansionPacketNotAllowed);
					log.Debug("IsAllowedToConnect deny access to expansion pack.");
					client.IsConnected = false;
					return false;
				}
			}

			/* Example to limit the connections from a certain IP range!
			if(client.Socket.RemoteEndPoint.ToString().StartsWith("192.168.0."))
			{
				client.Out.SendLoginDenied(eLoginError.AccountNoAccessAnyGame);
				return false;
			}
			 */


			/* Example to deny new connections on saturdays
			if(DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
			{
				client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);
				return false;
			}
			 */

			/* Example to deny new connections between 10am and 12am
			if(DateTime.Now.Hour >= 10 && DateTime.Now.Hour <= 12)
			{
				client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);
				return false;
			}
			 */

			DbAccount account = GameServer.Database.FindObjectByKey<DbAccount>(username);

			if (Properties.MAX_PLAYERS > 0 && string.IsNullOrEmpty(Properties.QUEUE_API_URI))
			{
				if (ClientService.ClientCount >= Properties.MAX_PLAYERS)
				{
					// GMs are still allowed to enter server
					if (account == null || (account.PrivLevel == 1 && account.Status <= 0))
					{
						// Normal Players will not be allowed over the max
						client.Out.SendLoginDenied(eLoginError.TooManyPlayersLoggedIn);
						log.Debug("IsAllowedToConnect deny access due to too many players.");
						client.IsConnected = false;
						return false;
					}
			
				}
			}

			if (Properties.STAFF_LOGIN)
			{
				if (account == null || account.PrivLevel == 1)
				{
					// GMs are still allowed to enter server
					// Normal Players will not be allowed to Log in
					client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);
					log.Debug("IsAllowedToConnect deny access; staff only login");
					client.IsConnected = false;
					return false;
				}
			}
			
			if (Properties.TESTER_LOGIN)
			{
				if (account == null || !account.IsTester && account.PrivLevel == 1)
				{
					// Admins and Testers are still allowed to enter server
					// Normal Players will not be allowed to Log in
					client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);
					log.Debug("IsAllowedToConnect deny access; tester and staff only login");
					client.IsConnected = false;
					return false;
				}
			}
			
			if (Properties.FORCE_DISCORD_LINK)
			{
				if (account == null || account.PrivLevel == 1 && account.DiscordID is (null or ""))
				{
					// GMs are still allowed to enter server
					// Normal Players will not be allowed to Log in unless they have linked their Discord
					client.Out.SendLoginDenied(eLoginError.AccountNoAccessThisGame);
					log.Debug("Denied access, account is not linked to Discord");
					client.IsConnected = false;
					return false;
				}
			}

			if (!Properties.ALLOW_DUAL_LOGINS)
			{
				if ((account == null || account.PrivLevel == 1) && client.TcpEndpointAddress != "not connected")
				{
					GameClient otherClient = ClientService.GetClientWithSameIp(client);
					
					if (otherClient != null)
					{
						client.Out.SendLoginDenied(eLoginError.AccountAlreadyLoggedIntoOtherServer);
						log.Debug("IsAllowedToConnect deny access; dual login not allowed");
						client.IsConnected = false;
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Called when player enters the game for first time
		/// </summary>
		/// <param name="e">event</param>
		/// <param name="sender">GamePlayer object that has entered the game</param>
		/// <param name="args"></param>
		public virtual void OnGameEntered(DOLEvent e, object sender, EventArgs args)
		{
			StartImmunityTimer((GamePlayer)sender, ServerProperties.Properties.TIMER_GAME_ENTERED * 1000);
		}

		/// <summary>
		/// Called when player has changed the region
		/// </summary>
		/// <param name="e">event</param>
		/// <param name="sender">GamePlayer object that has changed the region</param>
		/// <param name="args"></param>
		public virtual void OnRegionChanged(DOLEvent e, object sender, EventArgs args)
		{
			StartImmunityTimer((GamePlayer)sender, ServerProperties.Properties.TIMER_REGION_CHANGED * 1000);
		}

		/// <summary>
		/// Called after player has released
		/// </summary>
		/// <param name="e">event</param>
		/// <param name="sender">GamePlayer that has released</param>
		/// <param name="args"></param>
		public virtual void OnReleased(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = (GamePlayer)sender;
			StartImmunityTimer(player, ServerProperties.Properties.TIMER_KILLED_BY_MOB * 1000);//When Killed by a Mob
		}

		/// <summary>
		/// Should be called whenever a player teleports to a new location
		/// </summary>
		/// <param name="player"></param>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		public virtual void OnPlayerTeleport(GamePlayer player, GameLocation source, DbTeleport destination)
		{
			// override this in order to do something, like set immunity, when a player teleports
		}

		/// <summary>
		/// Starts the immunity timer for a player
		/// </summary>
		/// <param name="player">player that gets immunity</param>
		/// <param name="duration">amount of milliseconds when immunity ends</param>
		public virtual void StartImmunityTimer(GamePlayer player, int duration)
		{
			if (duration > 0)
			{
				player.StartInvulnerabilityTimer(duration, m_invExpiredCallback);
			}
		}

		/// <summary>
		/// Holds the delegate called when PvP invulnerability is expired
		/// </summary>
		protected GamePlayer.InvulnerabilityExpiredCallback m_invExpiredCallback;

		/// <summary>
		/// Removes immunity from the players
		/// </summary>
		/// <player></player>
		public virtual void ImmunityExpiredCallback(GamePlayer player)
		{
			if (player.ObjectState != GameObject.eObjectState.Active) return;
			if (player.Client.IsPlaying == false) return;

			player.Out.SendMessage("Your temporary invulnerability timer has expired.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

			return;
		}


		public abstract bool IsSameRealm(GameLiving source, GameLiving target, bool quiet);
		public abstract bool IsAllowedCharsInAllRealms(GameClient client);
		public abstract bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet);
		public abstract bool IsAllowedToJoinGuild(GamePlayer source, Guild guild);
		public abstract bool IsAllowedToTrade(GameLiving source, GameLiving target, bool quiet);
		public abstract bool IsAllowedToUnderstand(GameLiving source, GamePlayer target);
		public abstract string RulesDescription();

		public virtual bool IsAllowedToMoveToBind(GamePlayer player)
		{
			return true;
		}

		public virtual bool CountsTowardsSlashLevel(DbCoreCharacter player)
		{
			return true;
		}

		/// <summary>
		/// Is attacker allowed to attack defender.
		/// </summary>
		/// <param name="attacker">living that makes attack</param>
		/// <param name="defender">attacker's target</param>
		/// <param name="quiet">should messages be sent</param>
		/// <returns>true if attack is allowed</returns>
		public virtual bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
		{
			if (attacker == null || defender == null)
				return false;

			//dead things can't attack
			if (!defender.IsAlive || !attacker.IsAlive)
				return false;

			GamePlayer playerAttacker = attacker as GamePlayer;
			GamePlayer playerDefender = defender as GamePlayer;

			// if Pet, let's define the controller once
			if (defender is GameNPC)
				if ((defender as GameNPC).Brain is IControlledBrain)
					playerDefender = ((defender as GameNPC).Brain as IControlledBrain).GetPlayerOwner();

			if (attacker is GameNPC)
				if ((attacker as GameNPC).Brain is IControlledBrain)
					playerAttacker = ((attacker as GameNPC).Brain as IControlledBrain).GetPlayerOwner();

			if (playerDefender != null && (playerDefender.Client.ClientState == GameClient.eClientState.WorldEnter || playerDefender.IsInvulnerableToAttack))
			{
				if (!quiet)
					MessageToLiving(attacker, defender.Name + " is entering the game and is temporarily immune to PvP attacks!");
				return false;
			}

			if (playerAttacker != null && playerDefender != null)
			{
				// Attacker immunity
				if (playerAttacker.IsInvulnerableToAttack)
				{
					if (quiet == false) MessageToLiving(attacker, "You can't attack players until your PvP invulnerability timer wears off!");
					return false;
				}

				// Defender immunity
				if (playerDefender.IsInvulnerableToAttack)
				{
					if (quiet == false) MessageToLiving(attacker, defender.Name + " is temporarily immune to PvP attacks!");
					return false;
				}
			}

			// PEACE NPCs can't be attacked/attack
			if (attacker is GameNPC)
				if ((((GameNPC)attacker).Flags & GameNPC.eFlags.PEACE) != 0)
					return false;
			if (defender is GameNPC)
				if ((((GameNPC)defender).Flags & GameNPC.eFlags.PEACE) != 0)
					return false;
			// Players can't attack mobs while they have immunity
			if (playerAttacker != null && defender != null)
			{
				if ((defender is GameNPC) && (playerAttacker.IsInvulnerableToAttack))
				{
					if (quiet == false) MessageToLiving(attacker, "You can't attack until your PvP invulnerability timer wears off!");
					return false;
				}
			}

			// GMs can't be attacked
			if (playerDefender != null && playerDefender.Client.Account.PrivLevel > 1)
				return false;

			//flame - Commenting out Safe Area check as it was causing lots of lock contention in the GetAreasOfSpot() code. We currently dont have safe-areas so this doesnt affect anything

			// // Safe area support for defender
			// if (defender.CurrentAreas is not null)
			// {
			// 	var defenderAreas = defender.CurrentAreas.ToList();
			// 	foreach (AbstractArea area in defenderAreas)
			// 	{
			// 		if (area is null) continue;

			// 		if (!area.IsSafeArea)
			// 			continue;

			// 		if (defender is not GamePlayer) continue;
			// 		if (quiet == false) MessageToLiving(attacker, "You can't attack someone in a safe area!");
			// 		return false;
			// 	}
			// }		

			// //safe area support for attacker
			// var attackerAreas = attacker.CurrentAreas.ToList();
			// foreach (AbstractArea area in attackerAreas)
			// {
			// 	if ((area.IsSafeArea) && (defender is GamePlayer) && (attacker is GamePlayer))
			// 	{
			// 		if (quiet == false) MessageToLiving(attacker, "You can't attack someone in a safe area!");
			// 		return false;
			// 	}

			// 	if ((area.IsSafeArea) && (attacker is GamePlayer))
			// 	{
			// 		if (quiet == false) MessageToLiving(attacker, "You can't attack someone in a safe area!");
			// 		return false;
			// 	}
			// }

			if (attacker is GameNPC attacknpc && defender is GameNPC defendnpc)
			{
				// Mobs can't attack keep guards
				if (defender is GameKeepGuard && attacker.Realm == 0)
					return false;

				// Town guards however can attack mobs
				if (attacknpc is GameGuard)
					return true;

				// Anything can attack pets
				if (defender is GameSummonedPet || defendnpc.Brain is ControlledNpcBrain)
					return true;

				// Pets can attack everything else
				if (attacknpc is GameSummonedPet || attacknpc.Brain is ControlledNpcBrain)
					return true;

				// Mobs can attack mobs only if they both have a faction
				if (defendnpc.Faction == null || attacknpc.Faction == null)
					return false;
			}

			// Checking for shadowed necromancer, can't be attacked.
			if (defender.ControlledBrain?.Body is NecromancerPet)
			{
				if (!quiet)
					MessageToLiving(attacker, "You can't attack a shadowed necromancer!");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Is caster allowed to cast a spell
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="target"></param>
		/// <param name="spell"></param>
		/// <param name="spellLine"></param>
		/// <returns>true if allowed</returns>
		public virtual bool IsAllowedToCastSpell(GameLiving caster, GameLiving target, Spell spell, SpellLine spellLine)
		{
			//we only allow certain spell targets to be cast when targeting a keep component
			//tolakram - live allows most damage spells to be cast on doors. This should be handled in spell handlers
			if (target is GameKeepComponent || target is GameKeepDoor || target is GameSiegeWeapon)
			{
				bool isAllowed = false;

				switch (spell.Target)
				{
					case eSpellTarget.SELF:
					case eSpellTarget.GROUP:
					case eSpellTarget.PET:
					case eSpellTarget.CONTROLLED:
					case eSpellTarget.REALM:
					case eSpellTarget.AREA:
					{
						isAllowed = true;
						break;
					}
					case eSpellTarget.ENEMY:
					{
						if (spell.Radius == 0)
						{
							switch (spell.SpellType)
							{
								case eSpellType.Archery:
								case eSpellType.Bolt:
								case eSpellType.Bomber:
								case eSpellType.DamageSpeedDecrease:
								case eSpellType.DirectDamage:
								case eSpellType.MagicalStrike:
								case eSpellType.SiegeArrow:
								case eSpellType.SummonTheurgistPet:
								case eSpellType.DirectDamageWithDebuff:
									isAllowed = true;
									break;
							}
						}

						// pbaoe
						if (spell.Radius > 0 && spell.Range == 0)
						{
							isAllowed = true;
						}

						break;
					}
				}

				if (!isAllowed && caster is GamePlayer playerCaster)
					playerCaster.Client.Out.SendMessage("You can't cast this spell on the " + target.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);

				return isAllowed;
			}

			return true;
		}

		public virtual bool IsAllowedToSpeak(GamePlayer source, string communicationType)
		{
			if (source.IsAlive == false)
			{
				MessageToLiving(source, "Hmmmm...you can't " + communicationType + " while dead!");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Is player allowed to bind
		/// </summary>
		/// <param name="player"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public virtual bool IsAllowedToBind(GamePlayer player, DbBindPoint point)
		{
			return true;
		}

		/// <summary>
		/// Is player allowed to make the item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public virtual bool IsAllowedToCraft(GamePlayer player, DbItemTemplate item)
		{
			return true;
		}

		/// <summary>
		/// Is player allowed to claim in this region
		/// </summary>
		/// <param name="player"></param>
		/// <param name="region"></param>
		/// <returns></returns>
		public virtual bool IsAllowedToClaim(GamePlayer player, Region region)
		{
			if (region.IsInstance)
			{
				return false;
			}

			return true;
		}

		public virtual bool IsAllowedToZone(GamePlayer player, Region region)
		{
			return true;
		}

		/// <summary>
		/// is player allowed to ride his personal mount ?
		/// </summary>
		/// <param name="player"></param>
		/// <returns>string representing why player is not allowed to mount, else empty string</returns>
		public virtual string ReasonForDisallowMounting(GamePlayer player)
		{
			// pre conditions
			if (!player.IsAlive) return "GamePlayer.UseSlot.CantMountWhileDead";
			if (player.Steed != null) return "GamePlayer.UseSlot.MustDismountBefore";

			// gm/admin overrides the other checks
			if (player.Client.Account.PrivLevel != (uint)ePrivLevel.Player) return string.Empty;

			// player restrictions
			if (player.IsMoving) return "GamePlayer.UseSlot.CantMountMoving";
			if (player.InCombat) return "GamePlayer.UseSlot.CantMountCombat";
			if (player.IsSitting) return "GamePlayer.UseSlot.CantCallMountSeated";
			if (player.IsStealthed) return "GamePlayer.UseSlot.CantMountStealthed";

			// You are carrying a relic ? You can't use a mount !
			if (GameRelic.IsPlayerCarryingRelic(player))
				return "GamePlayer.UseSlot.CantMountRelicCarrier";

			// zones checks:
			// white list: always allows
			string currentRegion = player.CurrentRegion.ID.ToString();
			if (ServerProperties.Properties.ALLOW_PERSONNAL_MOUNT_IN_REGIONS.Contains(currentRegion))
			{
				var regions = ServerProperties.Properties.ALLOW_PERSONNAL_MOUNT_IN_REGIONS.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var region in regions)
					if (region == currentRegion)
						return string.Empty;
			}

			// restrictions: dungeons, instances, capitals, rvr horses
			if (player.CurrentRegion.IsDungeon ||
				player.CurrentRegion.IsInstance ||
				player.CurrentRegion.IsCapitalCity)
				return "GamePlayer.UseSlot.CantMountHere";
			// perhaps need to be tweaked for PvPServerRules
			if (player.CurrentRegion.IsRvR && !player.ActiveHorse.IsSummonRvR)
				return "GamePlayer.UseSlot.CantSummonRvR";

			// sounds good !
			return string.Empty;

		}

		public virtual bool CanTakeFallDamage(GamePlayer player)
		{
			if (player.Client.Account.PrivLevel > 1)
				return false;

			if (player.IsInvulnerableToAttack)
				return false;

			if (player.CurrentRegion.IsHousing)
				return false; // Workaround: falling from houses should not produce damage

			return true;
		}

		public virtual long GetExperienceForLiving(int level)
		{
			level = (level < 0) ? 0 : level;

			// use exp table
			if (level < GameLiving.XPForLiving.Length)
				return GameLiving.XPForLiving[level];

			// use formula if level is not in exp table
			// long can hold values up to level 238
			if (level > 238)
				level = 238;

			double k1, k1_inc, k1_lvl;

			// noret: using these rules i was able to reproduce table from
			// http://www.daocweave.com/daoc/general/experience_table.htm
			if (level >= 35)
			{
				k1_lvl = 35;
				k1_inc = 0.2;
				k1 = 20;
			}
			else if (level >= 20)
			{
				k1_lvl = 20;
				k1_inc = 0.3334;
				k1 = 15;
			}
			else if (level >= 10)
			{
				k1_lvl = 10;
				k1_inc = 0.5;
				k1 = 10;
			}
			else
			{
				k1_lvl = 0;
				k1_inc = 1;
				k1 = 0;
			}

			long exp = (long)(Math.Pow(2, k1 + (level - k1_lvl) * k1_inc) * 5);
			if (exp < 0)
			{
				exp = 0;
			}

			return exp;
		}

		// Can a character use this item?
		public virtual bool CheckAbilityToUseItem(GameLiving living, DbItemTemplate item)
		{
			if (living == null || item == null)
				return false;

			GamePlayer player = living as GamePlayer;

			// GMs can equip everything
			if (player != null && player.Client.Account.PrivLevel > (uint)ePrivLevel.Player)
				return true;

			// allow usage of all house items
			if ((item.Object_Type == 0 || item.Object_Type >= (int)eObjectType._FirstHouse) && item.Object_Type <= (int)eObjectType._LastHouse)
				return true;

			// on some servers we may wish for dropped items to be used by all realms regardless of what is set in the db
			if (!ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
			{
				if (item.Realm != 0 && item.Realm != (int)living.Realm)
					return false;
			}

			// classes restriction.
			if (player != null && !string.IsNullOrEmpty(item.AllowedClasses))
			{
				if (!Util.SplitCSV(item.AllowedClasses, true).Contains(player.CharacterClass.ID.ToString()))
					return false;
			}

			//armor
			if (item.Object_Type >= (int)eObjectType._FirstArmor && item.Object_Type <= (int)eObjectType._LastArmor)
			{
				int armorAbility = -1;

				if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && item.Item_Type != (int)eEquipmentItems.HEAD)
				{
					switch (player.Realm) // Choose based on player rather than item region
					{
						case eRealm.Albion: armorAbility = living.GetAbilityLevel(Abilities.AlbArmor); break;
						case eRealm.Hibernia: armorAbility = living.GetAbilityLevel(Abilities.HibArmor); break;
						case eRealm.Midgard: armorAbility =  living.GetAbilityLevel(Abilities.MidArmor); break;
						default: break;
					}
				}
				else
				{
					switch ((eRealm)item.Realm)
					{
						case eRealm.Albion: armorAbility = living.GetAbilityLevel(Abilities.AlbArmor); break;
						case eRealm.Hibernia: armorAbility = living.GetAbilityLevel(Abilities.HibArmor); break;
						case eRealm.Midgard: armorAbility = living.GetAbilityLevel(Abilities.MidArmor); break;
						default: // use old system
							armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.AlbArmor));
							armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.HibArmor));
							armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.MidArmor));
							break;
					}
				}
				switch ((eObjectType)item.Object_Type)
				{
					case eObjectType.GenericArmor: return armorAbility >= ArmorLevel.GenericArmor;
					case eObjectType.Cloth: return armorAbility >= ArmorLevel.Cloth;
					case eObjectType.Leather: return armorAbility >= ArmorLevel.Leather;
					case eObjectType.Reinforced:
					case eObjectType.Studded: return armorAbility >= ArmorLevel.Studded;
					case eObjectType.Scale:
					case eObjectType.Chain: return armorAbility >= ArmorLevel.Chain;
					case eObjectType.Plate: return armorAbility >= ArmorLevel.Plate;
					default: return false;
				}
			}

			// non-armors
			string abilityCheck = null;
			string[] otherCheck = new string[0];

			//http://dol.kitchenhost.de/files/dol/Info/itemtable.txt
			switch ((eObjectType)item.Object_Type)
			{
				case eObjectType.GenericItem: return true;
				case eObjectType.GenericArmor: return true;
				case eObjectType.GenericWeapon: return true;
				case eObjectType.Staff: abilityCheck = Abilities.Weapon_Staves; break;
				case eObjectType.Fired: abilityCheck = Abilities.Weapon_Shortbows; break;
				case eObjectType.FistWraps: abilityCheck = Abilities.Weapon_FistWraps; break;
				case eObjectType.MaulerStaff: abilityCheck = Abilities.Weapon_MaulerStaff; break;

				//alb
				case eObjectType.CrushingWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Crushing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blunt; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Hammers; break;
							default: break;
						} 
					else abilityCheck = Abilities.Weapon_Crushing;
					break;
				case eObjectType.SlashingWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Swords; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Slashing;
					break;
				case eObjectType.ThrustWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Hibernia)
						abilityCheck = Abilities.Weapon_Piercing;
					else
						abilityCheck = Abilities.Weapon_Thrusting;
					break;
				case eObjectType.TwoHandedWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Hibernia)
						abilityCheck = Abilities.Weapon_LargeWeapons;
					else abilityCheck = Abilities.Weapon_TwoHanded;
					break;
				case eObjectType.PolearmWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Polearms; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_CelticSpear; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Spears; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Polearms;
					break;
				case eObjectType.Longbow:
					otherCheck = new string[] { Abilities.Weapon_Longbows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Crossbow: abilityCheck = Abilities.Weapon_Crossbow; break;
				case eObjectType.Flexible: abilityCheck = Abilities.Weapon_Flexible; break;
				//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown;break;

				//mid
				case eObjectType.Sword:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Swords; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Swords; 
					break;
				case eObjectType.Hammer:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Crushing; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Hammers; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blunt; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Hammers; 
					break;
				case eObjectType.LeftAxe:
				case eObjectType.Axe:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Axes; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Axes; 
					break;
				case eObjectType.Spear:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Polearms; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_CelticSpear; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Spears; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Spears; 
					break;
				case eObjectType.CompositeBow:
					otherCheck = new string[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Thrown: abilityCheck = Abilities.Weapon_Thrown; break;
				case eObjectType.HandToHand: abilityCheck = Abilities.Weapon_HandToHand; break;

				//hib
				case eObjectType.RecurvedBow:
					otherCheck = new string[] { Abilities.Weapon_RecurvedBows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Blades:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Swords; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Blades; 
					break;
				case eObjectType.Blunt:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Crushing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blunt; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Hammers; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Blunt;
					break;
				case eObjectType.Piercing:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Albion)
						abilityCheck = Abilities.Weapon_Thrusting;
					else abilityCheck = Abilities.Weapon_Piercing;
					break;
				case eObjectType.LargeWeapons:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Albion)
						abilityCheck = Abilities.Weapon_TwoHanded;
					else abilityCheck = Abilities.Weapon_LargeWeapons; break;
				case eObjectType.CelticSpear:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Polearms; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_CelticSpear; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Spears; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_CelticSpear;
					break;
				case eObjectType.Scythe: abilityCheck = Abilities.Weapon_Scythe; break;

				//misc
				case eObjectType.Magical: return true;
				case eObjectType.Shield: return living.GetAbilityLevel(Abilities.Shield) >= item.Type_Damage;
				case eObjectType.Bolt: abilityCheck = Abilities.Weapon_Crossbow; break;
				case eObjectType.Arrow: otherCheck = new string[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Longbows, Abilities.Weapon_RecurvedBows, Abilities.Weapon_Shortbows }; break;
				case eObjectType.Poison: return living.GetModifiedSpecLevel(Specs.Envenom) > 0;
				case eObjectType.Instrument: return living.HasAbility(Abilities.Weapon_Instruments);
					//TODO: different shield sizes
			}

			if (abilityCheck != null && living.HasAbility(abilityCheck))
				return true;

			foreach (string str in otherCheck)
				if (living.HasAbility(str))
					return true;

			return false;
		}

		/// <summary>
		/// Get object specialization level based on server type
		/// </summary>
		/// <param name="player">player whom specializations are checked</param>
		/// <param name="objectType">object type</param>
		/// <returns>specialization in object or 0</returns>
		public virtual int GetObjectSpecLevel(GamePlayer player, eObjectType objectType)
		{
			int res = 0;

			foreach (eObjectType obj in GetCompatibleObjectTypes(objectType))
			{
				int spec = player.GetModifiedSpecLevel(SkillBase.ObjectTypeToSpec(obj));
				if (res < spec)
					res = spec;
			}
			return res;
		}

		/// <summary>
		/// Get object specialization level based on server type
		/// </summary>
		/// <param name="player">player whom specializations are checked</param>
		/// <param name="objectType">object type</param>
		/// <returns>specialization in object or 0</returns>
		public virtual int GetBaseObjectSpecLevel(GamePlayer player, eObjectType objectType)
		{
			int res = 0;

			foreach (eObjectType obj in GetCompatibleObjectTypes(objectType))
			{
				int spec = player.GetBaseSpecLevel(SkillBase.ObjectTypeToSpec(obj));
				if (res < spec)
					res = spec;
			}
			return res;
		}

		/// <summary>
		/// Checks whether one object type is equal to another
		/// based on server type
		/// </summary>
		/// <param name="type1"></param>
		/// <param name="type2"></param>
		/// <returns>true if equals</returns>
		public virtual bool IsObjectTypesEqual(eObjectType type1, eObjectType type2)
		{
			foreach (eObjectType obj in GetCompatibleObjectTypes(type1))
			{
				if (obj == type2)
					return true;
			}
			return false;
		}

		#region GetCompatibleObjectTypes

		/// <summary>
		/// Holds arrays of compatible object types
		/// </summary>
		protected Hashtable m_compatibleObjectTypes = null;

		/// <summary>
		/// Translates object type to compatible object types based on server type
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <returns>An array of compatible object types</returns>
		protected virtual eObjectType[] GetCompatibleObjectTypes(eObjectType objectType)
		{
			if (m_compatibleObjectTypes == null)
			{
				m_compatibleObjectTypes = new Hashtable();
				m_compatibleObjectTypes[(int)eObjectType.Staff] = new eObjectType[] { eObjectType.Staff };
				m_compatibleObjectTypes[(int)eObjectType.Fired] = new eObjectType[] { eObjectType.Fired };

				m_compatibleObjectTypes[(int)eObjectType.FistWraps] = new eObjectType[] { eObjectType.FistWraps };
				m_compatibleObjectTypes[(int)eObjectType.MaulerStaff] = new eObjectType[] { eObjectType.MaulerStaff };

				//alb
				m_compatibleObjectTypes[(int)eObjectType.CrushingWeapon] = new eObjectType[] { eObjectType.CrushingWeapon, eObjectType.Blunt, eObjectType.Hammer };
				m_compatibleObjectTypes[(int)eObjectType.SlashingWeapon] = new eObjectType[] { eObjectType.SlashingWeapon, eObjectType.Blades, eObjectType.Sword, eObjectType.Axe };
				m_compatibleObjectTypes[(int)eObjectType.ThrustWeapon] = new eObjectType[] { eObjectType.ThrustWeapon, eObjectType.Piercing };
				m_compatibleObjectTypes[(int)eObjectType.TwoHandedWeapon] = new eObjectType[] { eObjectType.TwoHandedWeapon, eObjectType.LargeWeapons };
				m_compatibleObjectTypes[(int)eObjectType.PolearmWeapon] = new eObjectType[] { eObjectType.PolearmWeapon, eObjectType.CelticSpear, eObjectType.Spear };
				m_compatibleObjectTypes[(int)eObjectType.Flexible] = new eObjectType[] { eObjectType.Flexible };
				m_compatibleObjectTypes[(int)eObjectType.Longbow] = new eObjectType[] { eObjectType.Longbow };
				m_compatibleObjectTypes[(int)eObjectType.Crossbow] = new eObjectType[] { eObjectType.Crossbow };
				//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown; break;

				//mid
				m_compatibleObjectTypes[(int)eObjectType.Hammer] = new eObjectType[] { eObjectType.Hammer, eObjectType.CrushingWeapon, eObjectType.Blunt };
				m_compatibleObjectTypes[(int)eObjectType.Sword] = new eObjectType[] { eObjectType.Sword, eObjectType.SlashingWeapon, eObjectType.Blades };
				m_compatibleObjectTypes[(int)eObjectType.LeftAxe] = new eObjectType[] { eObjectType.LeftAxe };
				m_compatibleObjectTypes[(int)eObjectType.Axe] = new eObjectType[] { eObjectType.Axe, eObjectType.SlashingWeapon, eObjectType.Blades }; //eObjectType.LeftAxe removed
				m_compatibleObjectTypes[(int)eObjectType.HandToHand] = new eObjectType[] { eObjectType.HandToHand };
				m_compatibleObjectTypes[(int)eObjectType.Spear] = new eObjectType[] { eObjectType.Spear, eObjectType.CelticSpear, eObjectType.PolearmWeapon };
				m_compatibleObjectTypes[(int)eObjectType.CompositeBow] = new eObjectType[] { eObjectType.CompositeBow };
				m_compatibleObjectTypes[(int)eObjectType.Thrown] = new eObjectType[] { eObjectType.Thrown };

				//hib
				m_compatibleObjectTypes[(int)eObjectType.Blunt] = new eObjectType[] { eObjectType.Blunt, eObjectType.CrushingWeapon, eObjectType.Hammer };
				m_compatibleObjectTypes[(int)eObjectType.Blades] = new eObjectType[] { eObjectType.Blades, eObjectType.SlashingWeapon, eObjectType.Sword, eObjectType.Axe };
				m_compatibleObjectTypes[(int)eObjectType.Piercing] = new eObjectType[] { eObjectType.Piercing, eObjectType.ThrustWeapon };
				m_compatibleObjectTypes[(int)eObjectType.LargeWeapons] = new eObjectType[] { eObjectType.LargeWeapons, eObjectType.TwoHandedWeapon };
				m_compatibleObjectTypes[(int)eObjectType.CelticSpear] = new eObjectType[] { eObjectType.CelticSpear, eObjectType.Spear, eObjectType.PolearmWeapon };
				m_compatibleObjectTypes[(int)eObjectType.Scythe] = new eObjectType[] { eObjectType.Scythe };
				m_compatibleObjectTypes[(int)eObjectType.RecurvedBow] = new eObjectType[] { eObjectType.RecurvedBow };

				m_compatibleObjectTypes[(int)eObjectType.Shield] = new eObjectType[] { eObjectType.Shield };
				m_compatibleObjectTypes[(int)eObjectType.Poison] = new eObjectType[] { eObjectType.Poison };
				//TODO: case 45: abilityCheck = Abilities.instruments; break;
			}

			eObjectType[] res = (eObjectType[])m_compatibleObjectTypes[(int)objectType];
			if (res == null)
				return new eObjectType[0];
			return res;
		}

		#endregion

		/// <summary>
		/// Invoked on NPC death and deals out
		/// experience/realm points if needed
		/// </summary>
		/// <param name="killedNPC">npc that died</param>
		/// <param name="killer">killer</param>
		public virtual void OnNPCKilled(GameNPC killedNPC, GameObject killer)
		{
			System.Globalization.NumberFormatInfo format = System.Globalization.NumberFormatInfo.InvariantInfo;
			HybridDictionary XPGainerList = new HybridDictionary();

			lock (killedNPC.XPGainers.SyncRoot)
			{
				foreach (DictionaryEntry gainer in killedNPC.XPGainers)
				{
					XPGainerList.Add(gainer.Key, gainer.Value);
				}
			}

			#region Worth no experience

			//"This monster has been charmed recently and is worth no experience."
			string message = "You gain no experience from this kill!";
			if (killedNPC.CurrentRegion?.Time - GameNPC.CHARMED_NOEXP_TIMEOUT <
			    killedNPC.TempProperties.GetProperty<long>(GameNPC.CHARMED_TICK_PROP))
			{
				message = "This monster has been charmed recently and is worth no experience.";
			}

			if (!killedNPC.IsWorthReward)
			{
				foreach (DictionaryEntry de in XPGainerList)
				{
					GamePlayer player = de.Key as GamePlayer;
					if (player != null)
						player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}

				return;
			}

			#endregion

			#region Group/Total Damage

			float totalDamage = 0;
			Dictionary<Group, int> plrGrpExp = new Dictionary<Group, int>();
			Dictionary<Group, float> grpToDmgDict = new Dictionary<Group, float>();
			GamePlayer highestPlayer = null;
			bool isGroupInRange = false;
			Group highestDamageDealingGroup = null;

			//Collect the total damage
			foreach (DictionaryEntry de in XPGainerList)
			{
				totalDamage += (float) de.Value;
				GamePlayer player = de.Key as GamePlayer;

				//Check stipulations (this will ignore all pet damage)
				if (player == null || player.ObjectState != GameObject.eObjectState.Active ||
				    !player.IsWithinRadius(killedNPC, WorldMgr.MAX_EXPFORKILL_DISTANCE))
					continue;

				if (player.Group != null)
				{
					// checking to see if any group members are in range of the killer
					if (player != (killer as GamePlayer))
						isGroupInRange = true;

					if (plrGrpExp.ContainsKey(player.Group))
						plrGrpExp[player.Group] += 1;
					else
						plrGrpExp[player.Group] = 1;

					if (grpToDmgDict.ContainsKey(player.Group))
						grpToDmgDict[player.Group] += (float) de.Value;
					else
						grpToDmgDict.Add(player.Group, (float)de.Value);					
				}

				// tolakram: only prepare for xp challenge code if player is in a group
				if (highestPlayer == null || (player.Level > highestPlayer.Level))
					highestPlayer = player;
			}
			
			//get the highest damage dealing group
			if(grpToDmgDict.Count > 0)
				highestDamageDealingGroup = grpToDmgDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; 

			#endregion

			HashSet<GameLiving> livingsToAward = new();
			Diagnostics.StartPerfCounter("ReaperService-NPC-OnNPCKilled-XP-NPC("+killedNPC.GetHashCode()+")");

			foreach (DictionaryEntry de in XPGainerList)
			{
				if (de.Key is GameLiving living)
				{
					var player = living as GamePlayer;

					if (player != null)
					{
						BattleGroup clientBattleGroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);
						if (clientBattleGroup != null)
						{
							livingsToAward.Add(living);
						} 
						else
						{
							if (living.Group != null)
							{
								if(highestDamageDealingGroup != null && living.Group == highestDamageDealingGroup )
									livingsToAward.Add(living);
								else if(player != null)
								{
									player.Out.SendMessage($"Your group did not deal enough damage to claim this kill.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								}
							}
							else
								livingsToAward.Add(living);
						}
					}
				}
			}

			foreach (GameLiving living in livingsToAward)
			{
				DictionaryEntry de = new(living, XPGainerList[living]);
				AwardExperience(de, killedNPC, killer, totalDamage, plrGrpExp, isGroupInRange);

				if (living.Level > 36)
				{
					int min = (int) Math.Round(killedNPC.Level / 10d) - 3;
					if (min < 1) min = 1;
					int max = (int) Math.Round(killedNPC.Level / 5d);
					if (killedNPC.Level > 55)
					{
						min++;
						max++;
					}

					if (killedNPC.CurrentZone is {IsOF: true})
					{
						min *= 5;
						max *= 5;
					}

					AtlasROGManager.GenerateReward(living, Util.Random(min, max));
				}
			}

			Diagnostics.StopPerfCounter("ReaperService-NPC-OnNPCKilled-XP-NPC("+killedNPC.GetHashCode()+")");
		}

		private void AwardExperience(DictionaryEntry de, GameNPC killedNPC, GameObject killer, float totalDamage, Dictionary<Group, int> plrGrpExp, bool isGroupInRange)
		{
			System.Globalization.NumberFormatInfo format = System.Globalization.NumberFormatInfo.InvariantInfo;
			long npcExpValue = killedNPC.ExperienceValue;
			int npcRPValue = killedNPC.RealmPointsValue;
			int npcBPValue = killedNPC.BountyPointsValue;
			double npcExceedXPCapAmount = killedNPC.ExceedXPCapAmount;

			//Need to do this before hand so we only do it once - just in case if the player levels!
			double highestConValue = 0;

			GameLiving living = de.Key as GameLiving;
			GamePlayer player = living as GamePlayer;

			GamePlayer highestPlayer = player;
			if (player != null && player.Group != null)
			{
				foreach (GamePlayer gamePlayer in player.Group.GetPlayersInTheGroup())
				{
					if (gamePlayer.Level > highestPlayer.Level)
						highestPlayer = gamePlayer;
				}
			}
			
			highestConValue = highestPlayer != null ? highestPlayer.GetConLevel(killedNPC) : living.GetConLevel(killedNPC);

			if (living is NecromancerPet)
			{
				NecromancerPet necroPet = living as NecromancerPet;
				player = ((necroPet.Brain as IControlledBrain).Owner) as GamePlayer;
			}

			//Check stipulations
			if (living == null || living.ObjectState != GameObject.eObjectState.Active ||
			    !living.IsWithinRadius(killedNPC, WorldMgr.MAX_EXPFORKILL_DISTANCE))
				return;

			//Changed: people were getting penalized for their pets doing damage
			double damagePercent = (float) de.Value / totalDamage;

			#region Realm Points

			// realm points
			int rpCap = living.RealmPointsValue * 2;
			int realmPoints = 0;

			// Keep and Tower captures reward full RP and BP value to each player
			if (killedNPC is GuardLord)
			{
				realmPoints = npcRPValue;
			}
			else
			{
				realmPoints = (int) (npcRPValue * damagePercent);
				//rp bonuses from RR and Group
				//100% if full group,scales down according to player count in group and their range to target
				if (player != null && player.Group != null && plrGrpExp.ContainsKey(player.Group))
				{
					realmPoints = (int) (realmPoints * (1.0 + plrGrpExp[player.Group] * 0.5));
				}
			}

			if (realmPoints > rpCap && !(killedNPC is Doppelganger))
				realmPoints = rpCap;

			if (realmPoints > 0)
				living.GainRealmPoints(realmPoints);

			#endregion

			#region Bounty Points

			// bounty points

			int bpCap = living.BountyPointsValue * 2;
			int bountyPoints = 0;

			// Keep and Tower captures reward full RP and BP value to each player
			if (killedNPC is GuardLord)
			{
				bountyPoints = npcBPValue;
			}
			else
			{
				bountyPoints = (int) (npcBPValue * damagePercent);
			}

			if (bountyPoints > bpCap && !(killedNPC is Doppelganger))
				bountyPoints = bpCap;
			if (bountyPoints > 0)
				living.GainBountyPoints(bountyPoints);

			#endregion

			// experience points
			long xpReward = 0;
			long campBonus = 0;
			long groupExp = 0;
			long outpostXP = 0;

			if (player != null && (player.Group == null || !plrGrpExp.ContainsKey(player.Group)))
				xpReward = (long) (npcExpValue * damagePercent); // exp for damage percent
			else
				xpReward = npcExpValue;

			//xp should divided across all members in the group, per this article: https://camelot.allakhazam.com/story.html?story=491
			if (player != null && player.Group != null && player.Group.MemberCount > 1)
			{
				int scalingFactor = (int) Math.Ceiling((decimal) player.Group.MemberCount);
				long tmpxp = (long) (xpReward * (1 + 0.125 * GetUniqueClassCount(player.Group)));
				xpReward = tmpxp / scalingFactor;
				//xpReward /= scalingFactor;
			}

			// exp cap
			/*

			http://support.darkageofcamelot.com/kb/article.php?id=438

			Experience clamps have been raised from 1.1x a same level kill to 1.25x a same level kill.
			This change has two effects: it will allow lower level players in a group to gain more experience faster (15% faster),
			and it will also let higher level players (the 35-50s who tend to hit this clamp more often) to gain experience faster.
			 */
			long expCap = (long) (GameServer.ServerRules.GetExperienceForLiving(living.Level) *
				ServerProperties.Properties.XP_CAP_PERCENT / 100);

			if (player != null)
			{
				expCap = (long) (GameServer.ServerRules.GetExperienceForLiving(player.Level) *
					ServerProperties.Properties.XP_CAP_PERCENT / 100);

				if (player.Group != null && isGroupInRange)
				{
					// Optional group cap can be set different from standard player cap
					expCap = (long) (GameServer.ServerRules.GetExperienceForLiving(player.Level) *
						ServerProperties.Properties.XP_GROUP_CAP_PERCENT / 100);
				}
			}

			#region Challenge Code

			//let's check the con, for example if a level 50 kills a green, we want our level 1 to get green xp too
			/*
			 * http://www.camelotherald.com/more/110.shtml
			 * All group experience is divided evenly amongst group members, if they are in the same level range. What's a level range? One color range.
			 * If everyone in the group cons yellow to each other (or high blue, or low orange), experience will be shared out exactly evenly, with no leftover points.
			 * How can you determine a color range? Simple - Level divided by ten plus one. So, to a level 40 player (40/10 + 1), 36-40 is yellow, 31-35 is blue,
			 * 26-30 is green, and 25-less is gray. But for everyone in the group to get the maximum amount of experience possible, the encounter must be a challenge to
			 * the group. If the group has two people, the monster must at least be (con) yellow to the highest level member. If the group has four people, the monster
			 * must at least be orange. If the group has eight, the monster must at least be red.
			 *
			 * If "challenge code" has been activated, then the experience is divided roughly like so in a group of two (adjust the colors up if the group is bigger): If
			 * the monster was blue to the highest level player, each lower level group member will ROUGHLY receive experience as if they soloed a blue monster.
			 * Ditto for green. As everyone knows, a monster that cons gray to the highest level player will result in no exp for anyone. If the monster was high blue,
			 * challenge code may not kick in. It could also kick in if the monster is low yellow to the high level player, depending on the group strength of the pair.
			 */
			//xp challenge
			if (player != null && highestPlayer != null && highestConValue < 0)
			{
				//challenge success, the xp needs to be reduced to the proper con
				expCap = (long) (GameServer.ServerRules.GetExperienceForLiving(
					GameObject.GetLevelFromCon(player.Level, highestConValue)));
			}

			#endregion

			expCap = (long) (expCap * npcExceedXPCapAmount);

			if (xpReward > expCap)
				xpReward = expCap;

			if (player != null && player.Group != null && (player.XPLogState == eXPLogState.On ||
			                                               player.XPLogState == eXPLogState.Verbose))
			{
				player.Out.SendMessage(
					$"XP Award: {xpReward.ToString("N0", format)} | Group XP Cap: {expCap.ToString("N0", format)}",
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				double expPercent = ((double) (xpReward) / (double) (expCap)) * 100;
				player.Out.SendMessage($"% of Cap: {expPercent.ToString(".##")}%", eChatType.CT_System,
					eChatLoc.CL_SystemWindow);
				player.Out.SendMessage($"---------------------------------------------------",
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}

			#region Camp Bonus

			// average max camp bonus is somewhere between 50 and 60%
			double fullCampBonus = ServerProperties.Properties.MAX_CAMP_BONUS;
			if (killer.CurrentZone.IsDungeon)
				fullCampBonus = 1; //dungeon gives +100% camp xp

			double campBonusPerc = 0;

			if (GameLoop.GameLoopTime - killedNPC.SpawnTick >
			    1800000) // spawn of this NPC was more than 30 minutes ago -> full camp bonus
			{
				campBonusPerc = fullCampBonus;
				killedNPC.CampBonus = 0.98;
			}
			else
			{
				campBonusPerc = fullCampBonus * killedNPC.CampBonus;
				if (killedNPC.CampBonus >= 0.02)
					killedNPC.CampBonus -= 0.02; // decrease camp bonus by 2% per kill
			}

			//1.49 http://news-daoc.goa.com/view_patchnote_archive.php?id_article=2478
			//"Camp bonuses" have been substantially upped in dungeons. Now camp bonuses in dungeons are, on average, 20% higher than outside camp bonuses.
			if (killer.CurrentZone.IsDungeon)
				campBonusPerc *= 1.50;


			if (campBonusPerc < 0.01)
				campBonusPerc = 0;
			else if (campBonusPerc > fullCampBonus)
				campBonusPerc = fullCampBonus;

			campBonus = (long) (xpReward * campBonusPerc);

			#endregion

			#region Outpost Bonus

			//outpost XP
			//1.54 http://www.camelotherald.com/more/567.shtml
			//- Players now receive an exp bonus when fighting within 16,000
			//units of a keep controlled by your realm or your guild.
			//You get 20% bonus if your guild owns the keep or a 10% bonus
			//if your realm owns the keep.

			if (player != null)
			{
				AbstractGameKeep keep =
					GameServer.KeepManager.GetKeepCloseToSpot(living.CurrentRegionID, living, 16000);
				if (keep != null)
				{
					byte bonus = 0;
					if (keep.Guild != null && keep.Guild == player.Guild)
						bonus = 20;
					else if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_Normal &&
					         keep.Realm == living.Realm)
						bonus = 10;

					outpostXP = (xpReward / 100) * bonus;
				}

				//FIXME: [WARN] this is a guess, I do not know the real way this is applied
				//apply the keep bonus for experience
				if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Experience_5, living.Realm))
					outpostXP += (xpReward / 100) * 5;
				else if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Experience_3, living.Realm))
					outpostXP += (xpReward / 100) * 3;
			}

			#endregion

			if (xpReward > 0)
			{
				if (player != null)
				{
					if (player.XPLogState == eXPLogState.Verbose)
					{
						player.Out.SendMessage(
							$"% of Camp remaining: {(campBonusPerc * 100 / fullCampBonus).ToString("0.##")}%",
							eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}

					if (player.Group != null && plrGrpExp.ContainsKey(player.Group))
						groupExp +=
							(long) (0.125 * xpReward *
							        GetUniqueClassCount(player.Group) /*(int)plrGrpExp[player.Group]*/);

					// tolakram - remove this for now.  Correct calculation should be reduced XP based on damage pet did, not a flat reduction
					//if (player.ControlledNpc != null)
					//    xpReward = (long)(xpReward * 0.75);
				}

				//Ok we've calculated all the base experience.  Now let's add them all together.
				xpReward += (long) campBonus + groupExp + outpostXP;

				if (!living.IsAlive) //Dead living gets 25% exp only
					xpReward = (long) (xpReward * 0.25);

				//scale xp reward based off of # of groups who participated in the kill
				if (plrGrpExp.Count > 0)
					xpReward /= plrGrpExp.Count;

				if (player != null && (player.XPLogState == eXPLogState.On ||
				                       player.XPLogState == eXPLogState.Verbose))
				{
					double baseXP = xpReward - campBonus - groupExp - outpostXP;
					/*int scaleFactor = 1;
					if (player.Group?.MemberCount > 1)
						scaleFactor = player.Group.MemberCount;
					double softXPCap = (long)((GameServer.ServerRules.GetExperienceForLiving(highestPlayer.Level) * ServerProperties.Properties.XP_CAP_PERCENT / 100) / scaleFactor);
					if (player.CurrentRegion.IsRvR)
						softXPCap = (long)(softXPCap * ServerProperties.Properties.RvR_XP_RATE);
					else
						softXPCap = (long)(softXPCap * ServerProperties.Properties.XP_RATE);
					*/
					//Console.WriteLine($"Soft xp cap: {softXPCap} getexp: {GameServer.ServerRules.GetExperienceForLiving(Level)}");
					long softXPCap = (long) (GameServer.ServerRules.GetExperienceForLiving(living.Level) *
						ServerProperties.Properties.XP_CAP_PERCENT / 100);
					player.Out.SendMessage(
						$"Base XP: {baseXP.ToString("N0", format)} | Solo Cap : {softXPCap.ToString("N0", format)} | %Cap: {((double) ((baseXP) / (softXPCap)) * 100).ToString("0.##")}%",
						eChatType.CT_System, eChatLoc.CL_SystemWindow);

					if (player.XPLogState == eXPLogState.Verbose)
					{
						double campPercent = ((double) campBonus / (baseXP)) * 100.0;
						double groupPercent = ((double) groupExp / (baseXP)) * 100.0;
						double outpostPercent = ((double) outpostXP / (baseXP)) * 100.0;
						double levelPercent =
							((double) (player.Experience + xpReward - player.ExperienceForCurrentLevel) /
							 (player.ExperienceForNextLevel - player.ExperienceForCurrentLevel)) * 100;
						double relicXp =  (baseXP * (0.05 * RelicMgr.GetRelicCount(player.Realm)));
						double relicPercent = ((double) relicXp / (baseXP)) * 100.0;

						player.Out.SendMessage(
							$"XP needed: {player.ExperienceForNextLevel.ToString("N0", format)} | {levelPercent.ToString("0.##")}% done with current level",
							eChatType.CT_System, eChatLoc.CL_SystemWindow);
						player.Out.SendMessage(
							$"# of kills needed to level at this rate: {(player.ExperienceForNextLevel - player.Experience) / xpReward}",
							eChatType.CT_System, eChatLoc.CL_SystemWindow);

						if (campBonus > 0)
							player.Out.SendMessage(
								$"Camp: {campBonus.ToString("N0", format)} | {campPercent.ToString("0.##")}% bonus",
								eChatType.CT_System, eChatLoc.CL_SystemWindow);

						if (player.Group != null)
							player.Out.SendMessage(
								$"Group: {groupExp.ToString("N0", format)} | {groupPercent.ToString("0.##")}% bonus",
								eChatType.CT_System, eChatLoc.CL_SystemWindow);

						if (outpostXP > 0)
							player.Out.SendMessage(
								$"Outpost: {outpostXP.ToString("N0", format)} | {outpostPercent.ToString("0.##")}% bonus",
								eChatType.CT_System, eChatLoc.CL_SystemWindow);
						
						if (relicXp > 0)
							player.Out.SendMessage(
								$"Relic: {relicXp.ToString("N0", format)} | {relicPercent.ToString("0.##")}% bonus",
								eChatType.CT_System, eChatLoc.CL_SystemWindow);

						//player.Out.SendMessage($"Total Bonus: {((double)((campBonus + groupExp + outpostXP) / xpReward) * 100).ToString("0.##")}%", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
				}

				//XP Rate is handled in GainExperience
				living.GainExperience(eXPSource.NPC, xpReward, campBonus, groupExp, outpostXP, true,
					true, true);
			}
		}

		private int GetUniqueClassCount(Group group)
        {
			HashSet<eCharacterClass> groupClasses = new HashSet<eCharacterClass>();
            foreach (var player in group.GetPlayersInTheGroup().ToList())
            {
				groupClasses.Add((eCharacterClass)player.CharacterClass.ID);
            }
			return groupClasses.Count;
        }

		/// <summary>
		/// Called on living death that is not gameplayer or gamenpc
		/// </summary>
		/// <param name="killedLiving">The living object</param>
		/// <param name="killer">The killer object</param>
		public virtual void OnLivingKilled(GameLiving killedLiving, GameObject killer)
		{
			HybridDictionary XPGainerList = new HybridDictionary();
			lock (killedLiving.XPGainers.SyncRoot)
			{
				foreach (DictionaryEntry gainer in killedLiving.XPGainers)
				{
					XPGainerList.Add(gainer.Key, gainer.Value);
				}
			}
			
			bool dealNoXP = false;
			float totalDamage = 0;
			//Collect the total damage
			foreach (DictionaryEntry de in XPGainerList)
			{
				GameObject obj = (GameObject)de.Key;
				if (obj is GamePlayer)
				{
					//If a gameplayer with privlevel > 1 attacked the
					//mob, then the players won't gain xp ...
					if (((GamePlayer)obj).Client.Account.PrivLevel > 1 || ((GamePlayer)obj).isInBG)
					{
						dealNoXP = true;
						break;
					}
				}
				totalDamage += (float)de.Value;
			}
			
			if (dealNoXP || (killedLiving.ExperienceValue == 0 && killedLiving.RealmPointsValue == 0 && killedLiving.BountyPointsValue == 0))
			{
				return;
			}
			
			long ExpValue = killedLiving.ExperienceValue; 
			int RPValue = killedLiving.RealmPointsValue;
			int BPValue = killedLiving.BountyPointsValue;

			//Now deal the XP and RPs to all livings
			foreach (DictionaryEntry de in XPGainerList)
			{
				GameLiving living = de.Key as GameLiving;
				GamePlayer expGainPlayer = living as GamePlayer;
				if (living == null)
				{
					continue;
				}
				if (living.ObjectState != GameObject.eObjectState.Active)
				{
					continue;
				}
				/*
				 * http://www.camelotherald.com/more/2289.shtml
				 * Dead players will now continue to retain and receive their realm point credit
				 * on targets until they release. This will work for solo players as well as
				 * grouped players in terms of continuing to contribute their share to the kill
				 * if a target is being attacked by another non grouped player as well.
				 */
				//if (!living.Alive) continue;
				if (!living.IsWithinRadius(killedLiving, WorldMgr.MAX_EXPFORKILL_DISTANCE))
				{
					continue;
				}


				double damagePercent = (float)de.Value / totalDamage;
				if (!living.IsAlive)//Dead living gets 25% exp only
					damagePercent *= 0.25;

				// realm points
				int rpCap = living.RealmPointsValue * 2;
				int realmPoints = (int)(RPValue * damagePercent);
				//rp bonuses from RR and Group
				//20% if R1L0 char kills RR10,if RR10 char kills R1L0 he will get -20% bonus
				//100% if full group,scales down according to player count in group and their range to target
				if (living is GamePlayer)
				{
					GamePlayer killerPlayer = living as GamePlayer;
					if (killerPlayer.Group != null && killerPlayer.Group.MemberCount > 1)
					{
						lock (killerPlayer.Group)
						{
							int count = 0;
							foreach (GamePlayer player in killerPlayer.Group.GetPlayersInTheGroup())
							{
								if (!player.IsWithinRadius(killedLiving, WorldMgr.MAX_EXPFORKILL_DISTANCE)) continue;
								count++;
							}
							realmPoints = (int)(realmPoints * (1.0 + count * 0.125));
						}
					}
				}
				if (realmPoints > rpCap)
					realmPoints = rpCap;
				if (realmPoints != 0)
				{
					living.GainRealmPoints(realmPoints);
				}

				// bounty points
				int bpCap = living.BountyPointsValue * 2;
				int bountyPoints = (int)(BPValue * damagePercent);
				if (bountyPoints > bpCap)
					bountyPoints = bpCap;
				if (bountyPoints != 0)
				{
					living.GainBountyPoints(bountyPoints);
				}

				// experience
				// TODO: pets take 25% and owner gets 75%
				long xpReward = (long)(ExpValue * damagePercent); // exp for damage percent

				long expCap = (long)(living.ExperienceValue * 1.25);
				if (xpReward > expCap)
					xpReward = expCap;

				eXPSource xpSource = eXPSource.NPC;
				if (killedLiving is GamePlayer)
				{
					xpSource = eXPSource.Player;
				}

				if (xpReward > 0)
					living.GainExperience(xpSource, xpReward);

			}
		}

		/// <summary>
		/// Invoked on Player death and deals out
		/// experience/realm points if needed
		/// </summary>
		/// <param name="killedPlayer">player that died</param>
		/// <param name="killer">killer</param>
		public virtual void OnPlayerKilled(GamePlayer killedPlayer, GameObject killer)
		{
			if (ServerProperties.Properties.ENABLE_WARMAPMGR && killer is GamePlayer && killer.CurrentRegion.ID == 163)
				WarMapMgr.AddFight((byte)killer.CurrentZone.ID, killer.X, killer.Y, (byte)killer.Realm, (byte)killedPlayer.Realm);
			
			HybridDictionary XPGainerList = new HybridDictionary();
			lock (killedPlayer.XPGainers.SyncRoot)
			{
				foreach (DictionaryEntry gainer in killedPlayer.XPGainers)
				{
					XPGainerList.Add(gainer.Key, gainer.Value);
				}
			}

			killedPlayer.LastDeathRealmPoints = 0;
			// "player has been killed recently"
			if (killedPlayer.DeathTime + Properties.RP_WORTH_SECONDS > killedPlayer.PlayedTime)
			{
				foreach (DictionaryEntry gainer in XPGainerList)
				{
					if (gainer.Key is GamePlayer player)
					{
						player.Out.SendMessage(killedPlayer.Name + " has been killed recently and is worth no realm points!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						player.Out.SendMessage(killedPlayer.Name + " has been killed recently and is worth no experience!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					}
				}
				return;
			}

			
			bool dealNoXP = false;
			float totalDamage = 0;
			//Collect the total damage
			foreach (DictionaryEntry de in XPGainerList)
			{
				GameObject obj = (GameObject)de.Key;
				if (obj is GamePlayer)
				{
					//If a gameplayer with privlevel > 1 attacked the
					//mob, then the players won't gain xp ...
					if (((GamePlayer)obj).Client.Account.PrivLevel > 1)
					{
						dealNoXP = true;
						break;
					}
				}
				totalDamage += (float)de.Value;
			}

			if (dealNoXP)
			{
				foreach (DictionaryEntry de in XPGainerList)
				{
					GamePlayer player = de.Key as GamePlayer;
					if (player != null)
						player.Out.SendMessage("You gain no experience from this kill!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				return;
			}
			
			long playerExpValue = killedPlayer.ExperienceValue;
			playerExpValue = (long)(playerExpValue * ServerProperties.Properties.XP_RATE);
			int playerRPValue = killedPlayer.RealmPointsValue;
			int playerBPValue = 0;

			bool BG = false;
			if (!ServerProperties.Properties.ALLOW_BPS_IN_BGS)
			{
				foreach (AbstractGameKeep keep in GameServer.KeepManager.GetKeepsOfRegion(killedPlayer.CurrentRegionID))
				{
					if (keep.DBKeep.BaseLevel < 50)
					{
						BG = true;
						break;
					}
				}
			}
			if (!BG)
				playerBPValue = killedPlayer.BountyPointsValue;
			long playerMoneyValue = killedPlayer.MoneyValue;
			
			//check for conquest activity
			if (killer is GamePlayer kp)
			{
				if (ConquestService.ConquestManager.IsValidDefender(kp))
				{
					ConquestService.ConquestManager.AddDefender(kp); //add to list of active keep defenders
					playerRPValue = (int)(playerRPValue * 1.10); //10% more RPs while defending the keep
					ConquestService.ConquestManager.AwardDefenders(playerRPValue, kp);
				}
			}

			List<KeyValuePair<GamePlayer, int>> playerKillers = new List<KeyValuePair<GamePlayer, int>>();
            List<Group> groupsToAward = new List<Group>();
			List<GamePlayer> playersToAward = new List<GamePlayer>();

            //Now deal the XP and RPs to all livings
            foreach (DictionaryEntry de in XPGainerList)
			{
				GameLiving living = de.Key as GameLiving;
				GamePlayer expGainPlayer = living as GamePlayer;
				if (living == null) continue;
				if (living.ObjectState != GameObject.eObjectState.Active) continue;
				/*
				 * http://www.camelotherald.com/more/2289.shtml
				 * Dead players will now continue to retain and receive their realm point credit
				 * on targets until they release. This will work for solo players as well as
				 * grouped players in terms of continuing to contribute their share to the kill
				 * if a target is being attacked by another non grouped player as well.
				 */
				//if (!living.Alive) continue;
				if (!living.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE)) continue;
				
				//check for conquest activity
				if (living is GamePlayer lp)
				{
					if(ConquestService.ConquestManager.IsPlayerInConquestArea(lp) && lp.Realm != killedPlayer.Realm)
						ConquestService.ConquestManager.AddContributor(lp);
				}

				if (PredatorManager.PlayerIsActive(killedPlayer))
				{
					var reward = PredatorManager.CheckForPredatorEffort(killedPlayer, expGainPlayer);
					if (reward > 0)
					{
						killedPlayer.Out.SendMessage($"Your hunt fails, but your prey did not escape unharmed.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						killedPlayer.GainRealmPoints(reward);
					}
				}

				double damagePercent = (float)de.Value / totalDamage;

				if (expGainPlayer?.GetConLevel(killedPlayer) > -3)
				{
					expGainPlayer.KillStreak++;
					expGainPlayer.Out.SendMessage($"Kill Streak: {expGainPlayer.KillStreak}", eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
				}

				// realm points
				int rpCap = living.RealmPointsValue * 2;
				int realmPoints = (int)(playerRPValue * damagePercent);

                switch (expGainPlayer?.GetConLevel(killedPlayer))
                {
					case <= -3:
						rpCap = 0;
						expGainPlayer.Out.SendMessage("You shamefully killed a defenseless opponent and gain no realm points from this kill!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						break;
					case -2:
						rpCap /= 4;
						break;
					case -1:
						rpCap /= 2;
						break;
					default:
						break;
                }

				//moved to after realmPoints assignment so that dead players retain full RP
				if (!living.IsAlive)//Dead living gets 25% exp only
					damagePercent *= 0.25;

				//rp bonuses from RR and Group
				//20% if R1L0 char kills RR10,if RR10 char kills R1L0 he will get -20% bonus
				//100% if full group,scales down according to player count in group and their range to target
				if (living is GamePlayer)
				{
					GamePlayer killerPlayer = living as GamePlayer;
					//only gain rps in a battleground if you are under the cap
					DbBattleground bg = GameServer.KeepManager.GetBattleground(killerPlayer.CurrentRegionID);
					if (bg == null || (killerPlayer.RealmLevel < bg.MaxRealmLevel))
					{
						realmPoints = (int)(realmPoints * (1.0 + 2.0 * (killedPlayer.RealmLevel - killerPlayer.RealmLevel) / 900.0));
						if (killerPlayer.Group != null && killerPlayer.Group.MemberCount > 1)
						{
							lock (killerPlayer.Group)
							{
								int count = 0;
								foreach (GamePlayer player in killerPlayer.Group.GetPlayersInTheGroup())
								{
									if (!player.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE)) continue;
									count++;
								}
								realmPoints = (int)(realmPoints * (1.0 + count * 0.125));

							}
							if (!groupsToAward.Contains(killerPlayer.Group)){ groupsToAward.Add(killerPlayer.Group); }
						} else if (!playersToAward.Contains(killerPlayer))
                        {
							playersToAward.Add(killerPlayer);
						}
					}
					if (realmPoints > rpCap)
						realmPoints = rpCap;
					if (realmPoints > 0)
					{
						if (living is GamePlayer p)
						{
							killedPlayer.LastDeathRealmPoints += realmPoints;
							playerKillers.Add(new KeyValuePair<GamePlayer, int>(living as GamePlayer, realmPoints));
						}
						living.GainRealmPoints(realmPoints);
					}
				}

				// bounty points
				int bpCap = living.BountyPointsValue * 2;
				int bountyPoints = (int)(playerBPValue * damagePercent);

				switch (expGainPlayer?.GetConLevel(killedPlayer))
				{
					case <= -3:
						bpCap = 0;
						expGainPlayer.Out.SendMessage("You killed a defenseless opponent and gain no bps from this kill, you animal!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						break;
					case -2:
						bpCap /= 4;
						break;
					case -1:
						bpCap /= 2;
						break;
					default:
						break;
				}

				if (bountyPoints > bpCap)
					bountyPoints = bpCap;

				

				//FIXME: [WARN] this is guessed, i do not believe this is the right way, we will most likely need special messages to be sent
				//apply the keep bonus for bounty points
				if (killer != null)
				{
					if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_5, (eRealm)killer.Realm))
						bountyPoints += (bountyPoints / 100) * 5;
					else if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_3, (eRealm)killer.Realm))
						bountyPoints += (bountyPoints / 100) * 3;
				}

				if (bountyPoints > 0)
				{
					living.GainBountyPoints(bountyPoints);
				}

				// experience
				// TODO: pets take 25% and owner gets 75%
				long xpReward = (long)(playerExpValue * damagePercent); // exp for damage percent

				long expCap = (long)(living.ExperienceValue * ServerProperties.Properties.XP_PVP_CAP_PERCENT / 100);

				switch (expGainPlayer?.GetConLevel(killedPlayer))
				{
					case <= -3:
						expCap = 0;
						expGainPlayer.Out.SendMessage("No experience points awarded for killing greys, you degenerate.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						break;
					case -2:
						expCap /= 4;
						break;
					case -1:
						expCap /= 2;
						break;
					default:
						break;
				}

				if (xpReward > expCap)
					xpReward = expCap;

				//outpost XP
				//1.54 http://www.camelotherald.com/more/567.shtml
				//- Players now receive an exp bonus when fighting within 16,000
				//units of a keep controlled by your realm or your guild.
				//You get 20% bonus if your guild owns the keep or a 10% bonus
				//if your realm owns the keep.

				long outpostXP = 0;

				if (!BG && living is GamePlayer)
				{
					AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(living.CurrentRegionID, living, 16000);
					if (keep != null)
					{
						byte bonus = 0;
						if (keep.Guild != null && keep.Guild == (living as GamePlayer).Guild)
							bonus = 20;
						else if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_Normal &&
								 keep.Realm == living.Realm)
							bonus = 10;

						outpostXP = (xpReward / 100) * bonus;
					}
				}

				if(xpReward > 0)
					xpReward += outpostXP;

				living.GainExperience(eXPSource.Player, xpReward);

				//gold
				if (living is GamePlayer)
				{
					long money = (long)(playerMoneyValue * damagePercent);
					GamePlayer player = living as GamePlayer;
					if (player.GetSpellLine("Spymaster") != null)
					{
						money += 20 * money / 100;
					}
					
					int killBonus = (int)((0.05 * (player.KillStreak > 10 ? 10 : player.KillStreak)) * money);

					if (killBonus > 0)
					{
						money += killBonus;
						//long money = (long)(Money.GetMoney(0, 0, 17, 85, 0) * damagePercent * killedPlayer.Level / 50);
						player.AddMoney(money, "You receive {0} ("+ Money.GetShortString(killBonus) +" streak bonus)");
						InventoryLogging.LogInventoryAction(killer, player, eInventoryActionType.Other, money);
					}
					else
					{
						player.AddMoney(money, "You receive {0}");
						InventoryLogging.LogInventoryAction(killer, player, eInventoryActionType.Other, money);
					}
					
					
				}

				if (killedPlayer.ReleaseType != eReleaseType.Duel && expGainPlayer != null)
				{
					if (expGainPlayer.GetConLevel(killedPlayer) > -3)
					{
						var killerCheck = killer;
						//if main pet killed target, check using owner
						if (killer is GameSummonedPet pet && pet.Owner == expGainPlayer)
						{
							killerCheck = expGainPlayer;
						}
						//same check for subpets
						if(killer is GameSummonedPet {Owner: GameSummonedPet mainpet} && mainpet.Owner == expGainPlayer)
						{
							killerCheck = expGainPlayer;
						}

						switch ((eRealm)killedPlayer.Realm)
						{
							case eRealm.Albion:
								expGainPlayer.KillsAlbionPlayers++;
								expGainPlayer.Achieve(AchievementUtils.AchievementNames.Alb_Players_Killed);
								if (expGainPlayer == killerCheck )
								{
									expGainPlayer.KillsAlbionDeathBlows++;
									expGainPlayer.Achieve(AchievementUtils.AchievementNames.Alb_Deathblows);
									CheckSoloKills(eRealm.Albion, XPGainerList, expGainPlayer, totalDamage);
								}
								break;

							case eRealm.Hibernia:
								expGainPlayer.KillsHiberniaPlayers++;
								expGainPlayer.Achieve(AchievementUtils.AchievementNames.Hib_Players_Killed);
								if (expGainPlayer == killerCheck)
								{
									expGainPlayer.KillsHiberniaDeathBlows++;
									expGainPlayer.Achieve(AchievementUtils.AchievementNames.Hib_Deathblows);
									CheckSoloKills(eRealm.Hibernia, XPGainerList, expGainPlayer, totalDamage);
								}
								break;

							case eRealm.Midgard:
								expGainPlayer.KillsMidgardPlayers++;
								expGainPlayer.Achieve(AchievementUtils.AchievementNames.Mid_Players_Killed);
								if (expGainPlayer == killerCheck)
								{
									expGainPlayer.KillsMidgardDeathBlows++;
									expGainPlayer.Achieve(AchievementUtils.AchievementNames.Mid_Deathblows);
									CheckSoloKills(eRealm.Midgard, XPGainerList, expGainPlayer, totalDamage);
								}
								break;
						}
					}
				}
			}
            
            killedPlayer.DeathsPvP++;

			//for each group member, a 50% chance to get a ROG
            foreach (var grp in groupsToAward)
            {
				List<GamePlayer> players = new List<GamePlayer>();
				foreach (GamePlayer pla in grp.GetPlayersInTheGroup())
                {
                    if (!playersToAward.Contains(pla))
                    {
						playersToAward.Add(pla);
					}
					//players.Add(pla);
                }
				//GamePlayer playerToAward = players[Util.Random(players.Count - 1)];
				//Console.WriteLine($"Chosen player: {playerToAward}");
				//if (!playersToAward.Contains(playerToAward) ) playersToAward.Add(playerToAward);
            }

			//distribute ROGs

            foreach (var player in playersToAward)
            {
                if (player.Level < 35 || player.GetDistanceTo(killedPlayer) > WorldMgr.MAX_EXPFORKILL_DISTANCE || player.GetConLevel(killedPlayer) <= -3) continue;
                
                if (GameServer.Instance.Configuration.ServerType != EGameServerType.GST_PvP)
                {
	                AtlasROGManager.GenerateOrbAmount(player, Util.Random(50, 150));
                }

                int bonusRegion = 0;
                switch (ZoneBonusRotator.GetCurrentBonusRealm())
                {
	                case eRealm.Albion:
		                bonusRegion = 1;
		                break;
	                case eRealm.Hibernia:
		                bonusRegion = 200;
		                break;
	                case eRealm.Midgard:
		                bonusRegion = 100;
		                break;
                }
                
                if (player.CurrentZone.ZoneRegion.ID == bonusRegion && Util.Chance(10))
                {
	                var RRMod = (int)Math.Floor(killedPlayer.RealmLevel / 10d) * 3;
	                AtlasROGManager.GenerateROG(player, (byte)(player.Level + RRMod));
                }

                if (player.CurrentZone.ZoneRegion.ID == bonusRegion && Util.Chance(1))
                {
	                AtlasROGManager.GenerateBeetleCarapace(player);
                }
            }

            if (ServerProperties.Properties.LOG_PVP_KILLS && playerKillers.Count > 0)
			{
				try
				{
					foreach (KeyValuePair<GamePlayer, int> pair in playerKillers)
					{

						DOL.Database.DbPvpKillLog killLog = new DOL.Database.DbPvpKillLog();
						killLog.KilledIP = killedPlayer.Client.TcpEndpointAddress;
						killLog.KilledName = killedPlayer.Name;
						killLog.KilledRealm = GlobalConstants.RealmToName(killedPlayer.Realm);
						killLog.KillerIP = pair.Key.Client.TcpEndpointAddress;
						killLog.KillerName = pair.Key.Name;
						killLog.KillerRealm = GlobalConstants.RealmToName(pair.Key.Realm);
						killLog.RPReward = pair.Value;
						killLog.RegionName = killedPlayer.CurrentRegion.Description;
						killLog.IsInstance = killedPlayer.CurrentRegion.IsInstance;

						if (killedPlayer.Client.TcpEndpointAddress == pair.Key.Client.TcpEndpointAddress)
							killLog.SameIP = 1;

						GameServer.Database.AddObject(killLog);
					}
				}
				catch (System.Exception ex)
				{
					log.Error(ex);
				}
			}
		}

		private void CheckSoloKills(eRealm realm, HybridDictionary XPGainerList, GamePlayer playerToCheck, float totalDamage)
		{
			float calcDamage = 0f;
			
			//turns out all the pet damage is stored under the owner's key, so this only ever checks playerToCheck's value
			//but whatever I'm keeping it in case we need it later
			foreach (DictionaryEntry gainer in XPGainerList)
			{
				//check for pets
				if (gainer.Key is GameSummonedPet pet && pet.Owner == playerToCheck && gainer.Value is float)
				{
					calcDamage += (float)gainer.Value;
				}
				
				//check for subpets (bonedancer and animist)
				if (gainer.Key is GameSummonedPet subpet && subpet.Owner is GameSummonedPet masterpet &&
				    masterpet.Owner == playerToCheck && gainer.Value is float)
				{
					calcDamage += (float)gainer.Value;
				}


				if (gainer.Key == playerToCheck && gainer.Value is float value)
				{
					calcDamage += value;
				}
			}
			
			if (Math.Abs(calcDamage - totalDamage) < 1) //since we compare floats we have to account for weird floating points and do comparison within a tolerance instead
			{
				switch (realm)
				{
					case eRealm.Albion:
						playerToCheck.KillsAlbionSolo++;
						playerToCheck.Achieve(AchievementUtils.AchievementNames.Alb_Solo_Kills);
						break;
					case eRealm.Midgard:
						playerToCheck.KillsMidgardSolo++;
						playerToCheck.Achieve(AchievementUtils.AchievementNames.Mid_Solo_Kills);
						break;
					case eRealm.Hibernia:
						playerToCheck.KillsHiberniaSolo++;
						playerToCheck.Achieve(AchievementUtils.AchievementNames.Hib_Solo_Kills);
						break;
				}
			}
		}

		/// <summary>
		/// Gets the Realm of an living for name text coloring
		/// </summary>
		/// <param name="player"></param>
		/// <param name="target"></param>
		/// <returns>byte code of realm</returns>
		public virtual byte GetLivingRealm(GamePlayer player, GameLiving target)
		{
			if (player == null || target == null) return 0;

			// clients with priv level > 1 are considered friendly by anyone
			GamePlayer playerTarget = target as GamePlayer;
			if (playerTarget != null && playerTarget.Client.Account.PrivLevel > 1) return (byte)player.Realm;

			return (byte)target.Realm;
		}

		/// <summary>
		/// Gets the player name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The name of the target</returns>
		public virtual string GetPlayerName(GamePlayer source, GamePlayer target)
		{
			return target.Name;
		}

		/// <summary>
		/// Gets the player Realmrank 12 or 13 title
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The Realmranktitle of the target</returns>
		public virtual string GetPlayerPrefixName(GamePlayer source, GamePlayer target)
		{
			string language;

			try
			{
				language = source.Client.Account.Language;
			}
			catch
			{
				language = LanguageMgr.DefaultLanguage;
			}

			if (IsSameRealm(source, target, true) && target.RealmLevel >= 110)
				return target.RealmRankTitle(language);

			return string.Empty;
		}

		/// <summary>
		/// Gets the player last name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The last name of the target</returns>
		public virtual string GetPlayerLastName(GamePlayer source, GamePlayer target)
		{
			return target.LastName;
		}

		/// <summary>
		/// Gets the player guild name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The guild name of the target</returns>
		public virtual string GetPlayerGuildName(GamePlayer source, GamePlayer target)
		{
			return target.GuildName;
		}

		/// <summary>
		/// Gets the player's custom title based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The custom title of the target</returns>
		public virtual string GetPlayerTitle(GamePlayer source, GamePlayer target)
		{
			return target.CurrentTitle.GetValue(source, target);
		}

		/// <summary>
		/// Gets the player's Total Amount of Realm Points Based on Level, Realm Level of other constraints.
		/// </summary>
		/// <param name="source">The player</param>
		/// <param name="target"></param>
		/// <returns>The total pool of realm points !</returns>
		public virtual int GetPlayerRealmPointsTotal(GamePlayer source)
		{
			return source.Level > 19 ? Math.Max(1, source.RealmLevel) : source.RealmLevel;
		}

		/// <summary>
		/// Gets the server type color handling scheme
		///
		/// ColorHandling: this byte tells the client how to handle color for PC and NPC names (over the head)
		/// 0: standard way, other realm PC appear red, our realm NPC appear light green
		/// 1: standard PvP way, all PC appear red, all NPC appear with their level color
		/// 2: Same realm livings are friendly, other realm livings are enemy; nearest friend/enemy buttons work
		/// 3: standard PvE way, all PC friendly, realm 0 NPC enemy rest NPC appear light green
		/// 4: All NPC are enemy, all players are friendly; nearest friend button selects self, nearest enemy don't work at all
		/// </summary>
		/// <param name="client">The client asking for color handling</param>
		/// <returns>The color handling</returns>
		public virtual byte GetColorHandling(GameClient client)
		{
			return 0;
		}

		/// <summary>
		/// Formats player statistics.
		/// </summary>
		/// <param name="player">The player to read statistics from.</param>
		/// <returns>List of strings.</returns>
		public virtual IList<string> FormatPlayerStatistics(GamePlayer player)
		{
			List<string> stat = new List<string>();

			int total = 0;
			
			#region Players Killed
			//only show if there is a kill [by Suncheck]
			if ((player.KillsAlbionPlayers + player.KillsMidgardPlayers + player.KillsHiberniaPlayers) > 0)
			{
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.Title"));
				switch ((eRealm)player.Realm)
				{
					case eRealm.Albion:
						if (player.KillsMidgardPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.MidgardPlayer") + ": " + player.KillsMidgardPlayers.ToString("N0"));
						if (player.KillsHiberniaPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.HiberniaPlayer") + ": " + player.KillsHiberniaPlayers.ToString("N0"));
						total = player.KillsMidgardPlayers + player.KillsHiberniaPlayers;
						break;
					case eRealm.Midgard:
						if (player.KillsAlbionPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.AlbionPlayer") + ": " + player.KillsAlbionPlayers.ToString("N0"));
						if (player.KillsHiberniaPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.HiberniaPlayer") + ": " + player.KillsHiberniaPlayers.ToString("N0"));
						total = player.KillsAlbionPlayers + player.KillsHiberniaPlayers;
						break;
					case eRealm.Hibernia:
						if (player.KillsAlbionPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.AlbionPlayer") + ": " + player.KillsAlbionPlayers.ToString("N0"));
						if (player.KillsMidgardPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.MidgardPlayer") + ": " + player.KillsMidgardPlayers.ToString("N0"));
						total = player.KillsMidgardPlayers + player.KillsAlbionPlayers;
						break;
				}
				if (total > 0)
				{
					stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.TotalPlayers") + ": " + total.ToString("N0"));
					stat.Add(" ");
				}
				
				
			}
			#endregion
			
			#region Players Deathblows
			//only show if there is a kill [by Suncheck]
			if ((player.KillsAlbionDeathBlows + player.KillsMidgardDeathBlows + player.KillsHiberniaDeathBlows) > 0)
			{
				total = 0;
				switch ((eRealm)player.Realm)
				{
					case eRealm.Albion:
						if (player.KillsMidgardDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.MidgardPlayer") + ": " + player.KillsMidgardDeathBlows.ToString("N0"));
						if (player.KillsHiberniaDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.HiberniaPlayer") + ": " + player.KillsHiberniaDeathBlows.ToString("N0"));
						total = player.KillsMidgardDeathBlows + player.KillsHiberniaDeathBlows;
						break;
					case eRealm.Midgard:
						if (player.KillsAlbionDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.AlbionPlayer") + ": " + player.KillsAlbionDeathBlows.ToString("N0"));
						if (player.KillsHiberniaDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.HiberniaPlayer") + ": " + player.KillsHiberniaDeathBlows.ToString("N0"));
						total = player.KillsAlbionDeathBlows + player.KillsHiberniaDeathBlows;
						break;
					case eRealm.Hibernia:
						if (player.KillsAlbionDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.AlbionPlayer") + ": " + player.KillsAlbionDeathBlows.ToString("N0"));
						if (player.KillsMidgardDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.MidgardPlayer") + ": " + player.KillsMidgardDeathBlows.ToString("N0"));
						total = player.KillsMidgardDeathBlows + player.KillsAlbionDeathBlows;
						break;
				}
				if (total > 0)
				{
					stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language,
						"PlayerStatistic.Deathblows.TotalPlayers") + ": " + total.ToString("N0"));
				}
			}
			#endregion
			stat.Add(" ");
			#region Players Solo Kills
			//only show if there is a kill [by Suncheck]
			if ((player.KillsAlbionSolo + player.KillsMidgardSolo + player.KillsHiberniaSolo) > 0)
			{
				total = 0;
				switch ((eRealm)player.Realm)
				{
					case eRealm.Albion:
						if (player.KillsMidgardSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.MidgardPlayer") + ": " + player.KillsMidgardSolo.ToString("N0"));
						if (player.KillsHiberniaSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.HiberniaPlayer") + ": " + player.KillsHiberniaSolo.ToString("N0"));
						total = player.KillsMidgardSolo + player.KillsHiberniaSolo;
						break;
					case eRealm.Midgard:
						if (player.KillsAlbionSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.AlbionPlayer") + ": " + player.KillsAlbionSolo.ToString("N0"));
						if (player.KillsHiberniaSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.HiberniaPlayer") + ": " + player.KillsHiberniaSolo.ToString("N0"));
						total = player.KillsAlbionSolo + player.KillsHiberniaSolo;
						break;
					case eRealm.Hibernia:
						if (player.KillsAlbionSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.AlbionPlayer") + ": " + player.KillsAlbionSolo.ToString("N0"));
						if (player.KillsMidgardSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.MidgardPlayer") + ": " + player.KillsMidgardSolo.ToString("N0"));
						total = player.KillsMidgardSolo + player.KillsAlbionSolo;
						break;
				}

				if (total > 0)
				{
					stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language,
						"PlayerStatistic.Solo.TotalPlayers") + ": " + total.ToString("N0"));
				}
			}
			#endregion
			stat.Add(" ");
			#region Keeps
			//only show if there is a capture [by Suncheck]
			if ((player.CapturedKeeps + player.CapturedRelics) > 0)
			{
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Capture.Title"));
				//stat.Add("Relics Taken: " + player.RelicsTaken.ToString("N0"));
				//stat.Add("Albion Keeps Captured: " + player.CapturedAlbionKeeps.ToString("N0"));
				//stat.Add("Midgard Keeps Captured: " + player.CapturedMidgardKeeps.ToString("N0"));
				//stat.Add("Hibernia Keeps Captured: " + player.CapturedHiberniaKeeps.ToString("N0"));
				if (player.CapturedKeeps > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Capture.Keeps") + ": " + player.CapturedKeeps.ToString("N0"));
				//stat.Add("Keep Lords Slain: " + player.KeepLordsSlain.ToString("N0"));
				//stat.Add("Albion Towers Captured: " + player.CapturedAlbionTowers.ToString("N0"));
				//stat.Add("Midgard Towers Captured: " + player.CapturedMidgardTowers.ToString("N0"));
				//stat.Add("Hibernia Towers Captured: " + player.CapturedHiberniaTowers.ToString("N0"));
				if(player.CapturedRelics > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Capture.Relics") + ": " + player.CapturedRelics.ToString("N0"));
				//
				//if (player.CapturedTowers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Capture.Towers") + ": " + player.CapturedTowers.ToString("N0"));
				//
				//stat.Add("Tower Captains Slain: " + player.TowerCaptainsSlain.ToString("N0"));
				//stat.Add("Realm Guard Kills Albion: " + player.RealmGuardTotalKills.ToString("N0"));
				//stat.Add("Realm Guard Kills Midgard: " + player.RealmGuardTotalKills.ToString("N0"));
				//stat.Add("Realm Guard Kills Hibernia: " + player.RealmGuardTotalKills.ToString("N0"));
				//stat.Add("Total Realm Guard Kills: " + player.RealmGuardTotalKills.ToString("N0"));
			}
			#endregion
			stat.Add(" ");
			#region PvE
			//only show if there is a kill [by Suncheck]
			if ((player.KillsDragon + player.KillsEpicBoss + player.KillsLegion) > 0)
			{
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.Title"));
				if (player.KillsDragon > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.KillsDragon") + ": " + player.KillsDragon.ToString("N0"));
				if (player.KillsEpicBoss > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.KillsEpic") + ": " + player.KillsEpicBoss.ToString("N0"));
				if (player.KillsLegion > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.KillsLegion") + ": " + player.KillsLegion.ToString("N0"));
			}
			#endregion

			return stat;
		}

		/// <summary>
		/// Reset the keep with special server rules handling
		/// </summary>
		/// <param name="lord">The lord that was killed</param>
		/// <param name="killer">The lord's killer</param>
		public virtual void ResetKeep(GuardLord lord, GameObject killer)
		{
			PlayerMgr.UpdateStats(lord);
		}

		/// <summary>
		/// Experience a keep is worth when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual long GetExperienceForKeep(AbstractGameKeep keep)
		{
			return 0;
		}

		public virtual double GetExperienceCapForKeep(AbstractGameKeep keep)
		{
			return 1.0;
		}

		/// <summary>
		/// Realm points a keep is worth when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual int GetRealmPointsForKeep(AbstractGameKeep keep)
		{
			int value = 0;
			
			if (keep is GameKeep)
			{
				value = Properties.KEEP_RP_BASE + (keep.BaseLevel - 50) * Properties.KEEP_RP_MULTIPLIER;
			}

			value += ((keep.Level - Properties.STARTING_KEEP_LEVEL) * Properties.UPGRADE_MULTIPLIER);

			return value;
		}

		/// <summary>
		/// Bounty points a keep is worth when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual int GetBountyPointsForKeep(AbstractGameKeep keep)
		{
			return 0;
		}


		/// <summary>
		/// How much money does this keep reward when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual long GetMoneyValueForKeep(AbstractGameKeep keep)
		{
			return 0;
		}


		/// <summary>
		/// Is the player allowed to generate news
		/// </summary>
		/// <param name="player">the player</param>
		/// <returns>true if the player is allowed to generate news</returns>
		public virtual bool CanGenerateNews(GamePlayer player)
		{
			if (player.Client.Account.PrivLevel > 1)
				return false;

			return true;
		}

		/// <summary>
		/// Gets the NPC name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered NPC</param>
		/// <returns>The name of the target</returns>
		public virtual string GetNPCName(GamePlayer source, GameNPC target)
		{
			return target.Name;
		}

		/// <summary>
		/// Gets the NPC guild name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered NPC</param>
		/// <returns>The guild name of the target</returns>
		public virtual string GetNPCGuildName(GamePlayer source, GameNPC target)
		{
			return target.GuildName;
		}


		/// <summary>
		/// Get the items (merchant) list name for a lot marker in the specified region
		/// </summary>
		/// <param name="regionID"></param>
		/// <returns></returns>
		public virtual string GetLotMarkerListName(ushort regionID)
		{
			switch (regionID)
			{
				case 2:
					return "housing_alb_lotmarker";
				case 102:
					return "housing_mid_lotmarker";
				case 202:
					return "housing_hib_lotmarker";
				default:
					return "housing_custom_lotmarker";
			}
		}


		/// <summary>
		/// Send merchant window containing housing items that can be purchased by a player.  If this list is customized
		/// then the customized list must also be handled in BuyHousingItem
		/// </summary>
		/// <param name="player"></param>
		/// <param name="merchantType"></param>
		public virtual void SendHousingMerchantWindow(GamePlayer player, DOL.GS.PacketHandler.eMerchantWindowType merchantType)
		{
			switch (merchantType)
			{
				case eMerchantWindowType.HousingInsideShop:
				case eMerchantWindowType.HousingInsideMenu:
					player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorShopItems, merchantType);
					break;
				case eMerchantWindowType.HousingOutsideShop:
				case eMerchantWindowType.HousingOutsideMenu:
					player.Out.SendMerchantWindow(HouseTemplateMgr.OutdoorShopItems, merchantType);
					break;
				case eMerchantWindowType.HousingBindstoneHookpoint:
					switch (player.Realm)
					{
						case eRealm.Albion:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorBindstoneShopItemsAlb, merchantType);
							break;
						case eRealm.Midgard:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorBindstoneShopItemsMid, merchantType);
							break;
						case eRealm.Hibernia:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorBindstoneShopItemsHib, merchantType);
							break;
						default:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorBindstoneShopItems, merchantType);
							break;
					}
					break;
				case eMerchantWindowType.HousingCraftingHookpoint:
					player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorCraftShopItems, merchantType);
					break;
				case eMerchantWindowType.HousingNPCHookpoint:
					player.Out.SendMerchantWindow(HouseTemplateMgr.GetNpcShopItems(player), merchantType);
					break;
				case eMerchantWindowType.HousingVaultHookpoint:
					switch (player.Realm)
					{
						case eRealm.Albion:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorVaultShopItemsAlb, merchantType);
							break;
						case eRealm.Midgard:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorVaultShopItemsMid, merchantType);
							break;
						case eRealm.Hibernia:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorVaultShopItemsHib, merchantType);
							break;
						default:
							player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorVaultShopItems, merchantType);
							break;
					}
					break;
				case eMerchantWindowType.HousingDeedMenu:
					player.Out.SendMerchantWindow(/* TODO */HouseTemplateMgr.OutdoorMenuItems, eMerchantWindowType.HousingDeedMenu);
					break;
				default:
					player.Out.SendMessage("Unknown merchant type!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
					log.ErrorFormat("Unknown merchant type {0}", merchantType);
					break;
			}
		}


		/// <summary>
		/// Buys an item off a housing merchant.  If the list has been customized then this must be modified to
		/// match that customized list.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="slot"></param>
		/// <param name="count"></param>
		/// <param name="merchantType"></param>
		public virtual void BuyHousingItem(GamePlayer player, ushort slot, byte count, DOL.GS.PacketHandler.eMerchantWindowType merchantType)
		{
			MerchantTradeItems items = null;

			switch (merchantType)
			{
				case eMerchantWindowType.HousingInsideShop:
					items = HouseTemplateMgr.IndoorShopItems;
					break;
				case eMerchantWindowType.HousingOutsideShop:
					items = HouseTemplateMgr.OutdoorShopItems;
					break;
				case eMerchantWindowType.HousingBindstoneHookpoint:
					switch (player.Realm)
					{
						case eRealm.Albion:
							items = HouseTemplateMgr.IndoorBindstoneShopItemsAlb;
							break;
						case eRealm.Hibernia:
							items = HouseTemplateMgr.IndoorBindstoneShopItemsHib;
							break;
						case eRealm.Midgard:
							items = HouseTemplateMgr.IndoorBindstoneShopItemsMid;
							break;
						default:
							items = HouseTemplateMgr.IndoorBindstoneShopItems;
							break;
					}
					break;
				case eMerchantWindowType.HousingCraftingHookpoint:
					items = HouseTemplateMgr.IndoorCraftShopItems;
					break;
				case eMerchantWindowType.HousingNPCHookpoint:
					items = HouseTemplateMgr.GetNpcShopItems(player);
					break;
				case eMerchantWindowType.HousingVaultHookpoint:
					switch (player.Realm)
					{
						case eRealm.Albion:
							items = HouseTemplateMgr.IndoorVaultShopItemsAlb;
							break;
						case eRealm.Hibernia:
							items = HouseTemplateMgr.IndoorVaultShopItemsHib;
							break;
						case eRealm.Midgard:
							items = HouseTemplateMgr.IndoorVaultShopItemsMid;
							break;
						default:
							items = HouseTemplateMgr.IndoorVaultShopItems;
							break;
					}
					break;
			}

			GameMerchant.OnPlayerBuy(player, slot, count, items);
		}


		/// <summary>
		/// Get a housing hookpoint NPC
		/// </summary>
		/// <param name="house"></param>
		/// <param name="templateID"></param>
		/// <param name="heading"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public virtual GameNPC PlaceHousingNPC(DOL.GS.Housing.House house, DbItemTemplate item, IPoint3D location, ushort heading)
		{
			NpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(item.Bonus);

			try
			{
				string defaultClassType = ServerProperties.Properties.GAMENPC_DEFAULT_CLASSTYPE;

				if (npcTemplate == null || string.IsNullOrEmpty(npcTemplate.ClassType))
				{
					log.Warn("[Housing] null classtype in hookpoint attachment, using GAMENPC_DEFAULT_CLASSTYPE instead");
				}
				else
				{
					defaultClassType = npcTemplate.ClassType;
				}

				var npc = (GameNPC)Assembly.GetAssembly(typeof(GameServer)).CreateInstance(defaultClassType, false);
				if (npc == null)
				{
					foreach (Assembly asm in ScriptMgr.Scripts)
					{
						npc = (GameNPC)asm.CreateInstance(defaultClassType, false);
						if (npc != null) break;
					}
				}

				if (npc == null)
				{
					HouseMgr.log.Error("[Housing] Can't create instance of type: " + defaultClassType);
					return null;
				}

				npc.Model = 0;

				if (npcTemplate != null)
				{
					npc.LoadTemplate(npcTemplate);
				}
				else
				{
					npc.Size = 50;
					npc.Level = 50;
					npc.GuildName = "No Template Found";
				}

				if (npc.Model == 0)
				{
					// defaults if templates are missing
					if (house.Realm == eRealm.Albion)
					{
						npc.Model = (ushort)Util.Random(7, 8);
					}
					else if (house.Realm == eRealm.Midgard)
					{
						npc.Model = (ushort)Util.Random(160, 161);
					}
					else
					{
						npc.Model = (ushort)Util.Random(309, 310);
					}
				}

				// always set the npc realm to the house model realm
				npc.Realm = house.Realm;

				npc.Name = item.Name;
				npc.CurrentHouse = house;
				npc.InHouse = true;
				npc.OwnerID = item.Id_nb;
				npc.X = location.X;
				npc.Y = location.Y;
				npc.Z = location.Z;
				npc.Heading = heading;
				npc.CurrentRegionID = house.RegionID;
				if ((npc.Flags & GameNPC.eFlags.PEACE) == 0)
				{
					npc.Flags ^= GameNPC.eFlags.PEACE;
				}
				npc.AddToWorld();
				return npc;
			}
			catch (Exception ex)
			{
				log.Error("Error filling housing hookpoint using npc template ID " + item.Bonus, ex);
			}

			return null;
		}


		public virtual GameStaticItem PlaceHousingInteriorItem(DOL.GS.Housing.House house, DbItemTemplate item, IPoint3D location, ushort heading)
		{
			GameStaticItem hookpointObject = new GameStaticItem();
			hookpointObject.CurrentHouse = house;
			hookpointObject.InHouse = true;
			hookpointObject.OwnerID = item.Id_nb;
			hookpointObject.X = location.X;
			hookpointObject.Y = location.Y;
			hookpointObject.Z = location.Z;
			hookpointObject.Heading = heading;
			hookpointObject.CurrentRegionID = house.RegionID;
			hookpointObject.Name = item.Name;
			hookpointObject.Model = (ushort)item.Model;
			hookpointObject.AddToWorld();

			return hookpointObject;
		}

		/// <summary>
		/// This creates the housing consignment merchant attached to a house.
		/// You can override this to create your own consignment merchant derived from the standard merchant
		/// </summary>
		/// <returns></returns>
		public virtual GameConsignmentMerchant CreateHousingConsignmentMerchant(House house)
		{
			var m = new GameConsignmentMerchant();
			m.Name = "Consignment Merchant";
			return m;
		}

		/// <summary>
		/// Standard Rules For Player Level UP
		/// </summary>
		/// <param name="player"></param>
		/// <param name="previousLevel"></param>
		public virtual void OnPlayerLevelUp(GamePlayer player, int previousLevel)
		{
		}
		#region MessageToLiving
		/// <summary>
		/// Send system text message to system window
		/// </summary>
		/// <param name="living"></param>
		/// <param name="message"></param>
		public virtual void MessageToLiving(GameLiving living, string message)
		{
			MessageToLiving(living, message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
		/// <summary>
		/// Send custom text message to system window
		/// </summary>
		/// <param name="living"></param>
		/// <param name="message"></param>
		/// <param name="type"></param>
		public virtual void MessageToLiving(GameLiving living, string message, eChatType type)
		{
			MessageToLiving(living, message, type, eChatLoc.CL_SystemWindow);
		}
		/// <summary>
		/// Send custom text message to GameLiving
		/// </summary>
		/// <param name="living"></param>
		/// <param name="message"></param>
		/// <param name="type"></param>
		/// <param name="loc"></param>
		public virtual void MessageToLiving(GameLiving living, string message, eChatType type, eChatLoc loc)
		{
			if (living is GamePlayer)
				((GamePlayer)living).Out.SendMessage(message, type, loc);
		}
		#endregion
	}
}
