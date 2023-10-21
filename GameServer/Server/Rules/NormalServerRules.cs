using System.Collections;
using System.Linq;
using Core.AI.Brain;
using Core.Base.Enums;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Keeps;

namespace Core.GS.ServerRules
{
	[ServerRules(EGameServerType.GST_Normal)]
	public class NormalServerRules : AServerRules
	{
		public override string RulesDescription()
		{
			return "standard Normal server rules";
		}

		/// <summary>
		/// Invoked on NPC death and deals out
		/// experience/realm points if needed
		/// </summary>
		/// <param name="killedNPC">npc that died</param>
		/// <param name="killer">killer</param>
		public override void OnNPCKilled(GameNpc killedNPC, GameObject killer)
		{
			base.OnNPCKilled(killedNPC, killer); 	
		}

		public override bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
		{
			if (!base.IsAllowedToAttack(attacker, defender, quiet))
				return false;

			// if controlled NPC - do checks for owner instead
			if (attacker is GameNpc)
			{
				IControlledBrain controlled = ((GameNpc)attacker).Brain as IControlledBrain;
				if (controlled != null)
				{
                    attacker = controlled.GetLivingOwner();
					quiet = true; // silence all attacks by controlled npc
				}
			}
			if (defender is GameNpc)
			{
				IControlledBrain controlled = ((GameNpc)defender).Brain as IControlledBrain;
				if (controlled != null)
                    defender = controlled.GetLivingOwner();
			}

			//"You can't attack yourself!"
			if(attacker == defender)
			{
				if (quiet == false) MessageToLiving(attacker, "You can't attack yourself!");
				return false;
			}

			//Don't allow attacks on same realm members on Normal Servers
			if (attacker.Realm == defender.Realm && !(attacker is GamePlayer && ((GamePlayer)attacker).DuelTarget == defender))
			{
				// allow confused mobs to attack same realm
				if (attacker is GameNpc && (attacker as GameNpc).IsConfused)
					return true;

				if (attacker.Realm == 0)
				{
					return FactionMgr.CanLivingAttack(attacker, defender);
				}

				if(quiet == false) MessageToLiving(attacker, "You can't attack a member of your realm!");
				return false;
			}

			return true;
		}

		public override bool IsSameRealm(GameLiving source, GameLiving target, bool quiet)
		{
			if(source == null || target == null) 
				return false;

			// if controlled NPC - do checks for owner instead
			if (source is GameNpc)
			{
				IControlledBrain controlled = ((GameNpc)source).Brain as IControlledBrain;
				if (controlled != null)
				{
                    source = controlled.GetLivingOwner();
					quiet = true; // silence all attacks by controlled npc
				}
			}
			if (target is GameNpc)
			{
				IControlledBrain controlled = ((GameNpc)target).Brain as IControlledBrain;
				if (controlled != null)
                    target = controlled.GetLivingOwner();
			}

			if (source == target)
				return true;

			// clients with priv level > 1 are considered friendly by anyone
			if(target is GamePlayer && ((GamePlayer)target).Client.Account.PrivLevel > 1) return true;
			// checking as a gm, targets are considered friendly
			if (source is GamePlayer && ((GamePlayer)source).Client.Account.PrivLevel > 1) return true;

			//Peace flag NPCs are same realm
			if (target is GameNpc)
				if ((((GameNpc)target).Flags & ENpcFlags.PEACE) != 0)
					return true;

			if (source is GameNpc)
				if ((((GameNpc)source).Flags & ENpcFlags.PEACE) != 0)
					return true;

			if(source.Realm != target.Realm)
			{
				if(quiet == false) MessageToLiving(source, target.GetName(0, true) + " is not a member of your realm!");
				return false;
			}
			return true;
		}

		public override bool IsAllowedCharsInAllRealms(GameClient client)
		{
			if (client.Account.PrivLevel > 1)
				return true;
			if (ServerProperties.Properties.ALLOW_ALL_REALMS)
				return true;
			return false;
		}

		public override bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet)
		{
			if(source == null || target == null) return false;
			
			if (source.Realm != target.Realm)
			{
				if(quiet == false) MessageToLiving(source, "You can't group with a player from another realm!");
				return false;
			}

			return true;
		}


		public override bool IsAllowedToJoinGuild(GamePlayer source, GuildUtil guild)
		{
			if (source == null) 
				return false;

			if (ServerProperties.Properties.ALLOW_CROSS_REALM_GUILDS == false && guild.Realm != ERealm.None && source.Realm != guild.Realm)
			{
				return false;
			}

			return true;
		}

		public override bool IsAllowedToTrade(GameLiving source, GameLiving target, bool quiet)
		{

			if(source == null || target == null) return false;
			
			// clients with priv level > 1 are allowed to trade with anyone
			if(source is GamePlayer && target is GamePlayer)
			{
				if ((source as GamePlayer).Client.Account.PrivLevel > 1 || (target as GamePlayer).Client.Account.PrivLevel > 1)
					return true;
			}
			
			if((source as GamePlayer).NoHelp)
			{
				if(quiet == false) MessageToLiving(source, "You have renounced to any kind of help!");
				if(quiet == false) MessageToLiving(target, "This player has chosen to receive no help!");
				return false;
			}
			
			if((target as GamePlayer).NoHelp)
			{
				if(quiet == false) MessageToLiving(target, "You have renounced to any kind of help!");
				if(quiet == false) MessageToLiving(source, "This player has chosen to receive no help!");
				return false;
			}

			//Peace flag NPCs can trade with everyone
			if (target is GameNpc)
				if ((((GameNpc)target).Flags & ENpcFlags.PEACE) != 0)
					return true;

			if (source is GameNpc)
				if ((((GameNpc)source).Flags & ENpcFlags.PEACE) != 0)
					return true;

			if(source.Realm != target.Realm)
			{
				if(quiet == false) MessageToLiving(source, "You can't trade with enemy realm!");
				return false;
			}
			return true;
		}

		public override bool IsAllowedToUnderstand(GameLiving source, GamePlayer target)
		{
			if(source == null || target == null) return false;

			if (source.CurrentRegionID == 27) return true;

			// clients with priv level > 1 are allowed to talk and hear anyone
			if(source is GamePlayer && ((GamePlayer)source).Client.Account.PrivLevel > 1) return true;
			if(target.Client.Account.PrivLevel > 1) return true;

			//Peace flag NPCs can be understood by everyone

			if (source is GameNpc)
				if ((((GameNpc)source).Flags & ENpcFlags.PEACE) != 0)
					return true;

			if(source.Realm > 0 && source.Realm != target.Realm) return false;
			return true;
		}

		/// <summary>
		/// Is player allowed to bind
		/// </summary>
		/// <param name="player"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public override bool IsAllowedToBind(GamePlayer player, DbBindPoint point)
		{
			if (point.Realm == 0) return true;
			return player.Realm == (ERealm)point.Realm;
		}

		/// <summary>
		/// Is player allowed to make the item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public override bool IsAllowedToCraft(GamePlayer player, DbItemTemplate item)
		{
			return player.Realm == (ERealm)item.Realm || (item.Realm == 0 && ServerProperties.Properties.ALLOW_CRAFT_NOREALM_ITEMS);
		}

		/// <summary>
		/// Translates object type to compatible object types based on server type
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <returns>An array of compatible object types</returns>
		protected override EObjectType[] GetCompatibleObjectTypes(EObjectType objectType)
		{
			if(m_compatibleObjectTypes == null)
			{
				m_compatibleObjectTypes = new Hashtable();
				m_compatibleObjectTypes[(int)EObjectType.Staff] = new EObjectType[] { EObjectType.Staff };
				m_compatibleObjectTypes[(int)EObjectType.Fired] = new EObjectType[] { EObjectType.Fired };
                m_compatibleObjectTypes[(int)EObjectType.MaulerStaff] = new EObjectType[] { EObjectType.MaulerStaff };
				m_compatibleObjectTypes[(int)EObjectType.FistWraps] = new EObjectType[] { EObjectType.FistWraps };

				//alb
				m_compatibleObjectTypes[(int)EObjectType.CrushingWeapon]  = new EObjectType[] { EObjectType.CrushingWeapon };
				m_compatibleObjectTypes[(int)EObjectType.SlashingWeapon]  = new EObjectType[] { EObjectType.SlashingWeapon };
				m_compatibleObjectTypes[(int)EObjectType.ThrustWeapon]    = new EObjectType[] { EObjectType.ThrustWeapon };
				m_compatibleObjectTypes[(int)EObjectType.TwoHandedWeapon] = new EObjectType[] { EObjectType.TwoHandedWeapon };
				m_compatibleObjectTypes[(int)EObjectType.PolearmWeapon]   = new EObjectType[] { EObjectType.PolearmWeapon };
				m_compatibleObjectTypes[(int)EObjectType.Flexible]        = new EObjectType[] { EObjectType.Flexible };
				m_compatibleObjectTypes[(int)EObjectType.Longbow]         = new EObjectType[] { EObjectType.Longbow };
				m_compatibleObjectTypes[(int)EObjectType.Crossbow]        = new EObjectType[] { EObjectType.Crossbow };
				//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown; break;                                         

				//mid
				m_compatibleObjectTypes[(int)EObjectType.Hammer]       = new EObjectType[] { EObjectType.Hammer };
				m_compatibleObjectTypes[(int)EObjectType.Sword]        = new EObjectType[] { EObjectType.Sword };
				m_compatibleObjectTypes[(int)EObjectType.LeftAxe]      = new EObjectType[] { EObjectType.LeftAxe };
				m_compatibleObjectTypes[(int)EObjectType.Axe]          = new EObjectType[] { EObjectType.Axe, EObjectType.LeftAxe };
				m_compatibleObjectTypes[(int)EObjectType.HandToHand]   = new EObjectType[] { EObjectType.HandToHand };
				m_compatibleObjectTypes[(int)EObjectType.Spear]        = new EObjectType[] { EObjectType.Spear };
				m_compatibleObjectTypes[(int)EObjectType.CompositeBow] = new EObjectType[] { EObjectType.CompositeBow };
				m_compatibleObjectTypes[(int)EObjectType.Thrown]       = new EObjectType[] { EObjectType.Thrown };

				//hib
				m_compatibleObjectTypes[(int)EObjectType.Blunt]        = new EObjectType[] { EObjectType.Blunt };
				m_compatibleObjectTypes[(int)EObjectType.Blades]       = new EObjectType[] { EObjectType.Blades };
				m_compatibleObjectTypes[(int)EObjectType.Piercing]     = new EObjectType[] { EObjectType.Piercing };
				m_compatibleObjectTypes[(int)EObjectType.LargeWeapons] = new EObjectType[] { EObjectType.LargeWeapons };
				m_compatibleObjectTypes[(int)EObjectType.CelticSpear]  = new EObjectType[] { EObjectType.CelticSpear };
				m_compatibleObjectTypes[(int)EObjectType.Scythe]       = new EObjectType[] { EObjectType.Scythe };
				m_compatibleObjectTypes[(int)EObjectType.RecurvedBow]  = new EObjectType[] { EObjectType.RecurvedBow };

				m_compatibleObjectTypes[(int)EObjectType.Shield]       = new EObjectType[] { EObjectType.Shield };
				m_compatibleObjectTypes[(int)EObjectType.Poison]       = new EObjectType[] { EObjectType.Poison };
				//TODO: case 45: abilityCheck = Abilities.instruments; break;
			}

			EObjectType[] res = (EObjectType[])m_compatibleObjectTypes[(int)objectType];
			if(res == null)
				return new EObjectType[0];
			return res;
		}

		/// <summary>
		/// Gets the player name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The name of the target</returns>
		public override string GetPlayerName(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.Name;

			return source.RaceToTranslatedName(target.Race, target.Gender);
		}

		/// <summary>
		/// Gets the player last name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The last name of the target</returns>
		public override string GetPlayerLastName(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.LastName;

			return target.RealmRankTitle(source.Client.Account.Language);
		}

		/// <summary>
		/// Gets the player guild name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The guild name of the target</returns>
		public override string GetPlayerGuildName(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.GuildName;

			return string.Empty;
		}
	
		/// <summary>
		/// Gets the player's custom title based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The custom title of the target</returns>
		public override string GetPlayerTitle(GamePlayer source, GamePlayer target)
		{
			if (IsSameRealm(source, target, true))
				return target.CurrentTitle.GetValue(source, target);
			
			return string.Empty;
		}

		/// <summary>
		/// Reset the keep with special server rules handling
		/// </summary>
		/// <param name="lord">The lord that was killed</param>
		/// <param name="killer">The lord's killer</param>
		public override void ResetKeep(GuardLord lord, GameObject killer)
		{
			base.ResetKeep(lord, killer);
			lord.Component.Keep.Reset((ERealm)killer.Realm);
			
			if (ConquestService.ConquestManager.ActiveObjective != null && ConquestService.ConquestManager.ActiveObjective.Keep == lord.Component.Keep)
			{
				ConquestService.ConquestManager.ConquestCapture(ConquestService.ConquestManager.ActiveObjective.Keep);
			}
			
			if (ConquestService.ConquestManager.GetSecondaryObjectives().FirstOrDefault(conq => conq.Keep == lord.Component.Keep) != null)
			{
				ConquestService.ConquestManager.ConquestSubCapture(lord.Component.Keep);
			}
		}
	}
}
