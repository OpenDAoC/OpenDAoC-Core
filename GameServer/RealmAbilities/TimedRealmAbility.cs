using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;
using Core.GS.Packets;

namespace Core.GS.RealmAbilities
{
	public class TimedRealmAbility : RealmAbility
	{

		public TimedRealmAbility(DbAbility ability, int level) : base(ability, level) { }

		public override int CostForUpgrade(int level)
		{
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch(level)
				{
					case 0: return 5;
					case 1: return 5;
					case 2: return 5;
					case 3: return 7;
					case 4: return 8;
					default: return 1000;
				}
			}
			else
			{
				return (level + 1) * 5;
			}

			
		}

		public override bool CheckRequirement(GamePlayer player)
		{
			return true;
		}

		public virtual int GetReUseDelay(int level)
		{
			return 0;
		}

		protected string FormatTimespan(int seconds)
		{
			if (seconds >= 60)
			{
				return String.Format("{0:00}:{1:00} min", seconds / 60, seconds % 60);
			}
			else
			{
				return seconds + " sec";
			}
		}

		public virtual void AddReUseDelayInfo(IList<string> list)
		{
			for (int i = 1; i <= MaxLevel; i++)
			{
				int reUseTime = GetReUseDelay(i);
				list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "RealmAbility.AddReUseDelayInfo.Every", i, ((reUseTime == 0) ? "always" : FormatTimespan(reUseTime))));
			}
		}

		public virtual void AddEffectsInfo(IList<string> list)
		{
		}

		public override int MaxLevel
		{
			get
			{
				if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
				{
					return 5;
				}
				else
				{
					return 3;
				}
			}
		}

		public override void AddDelve(ref MiniDelveWriter w)
		{
			base.AddDelve(ref w);

			for (int i = 1; i <= MaxLevel; i++)
			{
				w.AddKeyValuePair(string.Format("ReuseTimer_{0}", i), GetReUseDelay(i));
			}
		}

		public virtual void DisableSkill(GameLiving living)
		{
			living.DisableSkill(this, GetReUseDelay(Level) * 1000);
		}

		public override IList<string> DelveInfo
		{
			get
			{
				IList<string> list = base.DelveInfo;
				int size = list.Count;
				AddEffectsInfo(list);

				if (list.Count > size)
				{ // something was added
					list.Insert(size, "");	// add empty line
				}

				size = list.Count;
				AddReUseDelayInfo(list);
				if (list.Count > size)
				{ // something was added
					list.Insert(size, "");	// add empty line
				}

				return list;
			}
		}


		/// <summary>
		/// Send spell effect animation on caster and send messages
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spellEffect"></param>
		/// <param name="success"></param>
		public virtual void SendCasterSpellEffectAndCastMessage(GameLiving caster, ushort spellEffect, bool success)
		{
			foreach (GamePlayer player in caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellEffectAnimation(caster, caster, spellEffect, 0, false, success ? (byte)1 : (byte)0);

				if ( caster.IsWithinRadius( player, WorldMgr.INFO_DISTANCE ) )
				{
					if (player == caster)
					{
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility.SendCasterSpellEffectAndCastMessage.You", m_name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}
					else
					{
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility.SendCasterSpellEffectAndCastMessage.Caster", caster.Name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}
				}
			}
		}
		
		public virtual void SendCasterSpellEffect(GameLiving caster, ushort spellEffect, bool success)
		{
			foreach (GamePlayer player in caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellEffectAnimation(caster, caster, spellEffect, 0, false, success ? (byte)1 : (byte)0);
			}
		}


		/// <summary>
		/// Sends cast message to environment
		/// </summary>
		protected virtual void SendCastMessage(GameLiving caster)
		{
			foreach (GamePlayer player in caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if ( caster.IsWithinRadius( player, WorldMgr.INFO_DISTANCE ) )
				{
					if (player == caster)
					{
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility.SendCastMessage.YouCast", m_name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}
					else
					{
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility.SendCastMessage.PlayerCasts", player.Name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}
				}
			}
		}

		/// <summary>
		/// Checks for any of the given conditions and returns true if there was any
		/// prints messages
		/// </summary>
		/// <param name="living"></param>
		/// <param name="bitmask"></param>
		/// <returns></returns>
		public virtual bool CheckPreconditions(GameLiving living, long bitmask)
		{
			GamePlayer player = living as GamePlayer;
			if ((bitmask & DEAD) != 0 && !living.IsAlive)
			{
				if (player != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.Dead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return true;
			}
			if ((bitmask & MEZZED) != 0 && living.IsMezzed)
			{
				if (player != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.Mesmerized"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return true;
			}
			if ((bitmask & STUNNED) != 0 && living.IsStunned)
			{
				if (player != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.Stunned"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return true;
			}
			if ((bitmask & SITTING) != 0 && living.IsSitting)
			{
				if (player != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.Sitting"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return true;
			}
			if ((bitmask & INCOMBAT) != 0 && living.InCombat)
			{
				if (player != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.Combat"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return true;
			}
			if ((bitmask & NOTINCOMBAT) != 0 && !living.InCombat)
			{
				if (player != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.BeInCombat"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return true;
			}
			if ((bitmask & STEALTHED) != 0 && living.IsStealthed)
			{
				if (player != null)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.Stealthed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				return true;
			}
			if (player != null && (bitmask & NOTINGROUP) != 0 && player.Group == null)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RealmAbility..CheckPreconditions.BeInGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return true;
			}
			return false;
		}

		/*
		 * Stored in hex, different values in binary
		 * e.g.
		 * 16|8|4|2|1
		 * ----------
		 * 1
		 * 0|0|0|0|1 stored as 0x00000001
		 * 2
		 * 0|0|0|1|0 stored as 0x00000002
		 * 4
		 * 0|0|1|0|0 stored as 0x00000004
		 * 8
		 * 0|1|0|0|0 stored as 0x00000008
		 * 16
		 * 1|0|0|0|0 stored as 0x00000010
		 */
		public const long DEAD = 0x00000001;
		public const long SITTING = 0x00000002;
		public const long MEZZED = 0x00000004;
		public const long STUNNED = 0x00000008;
		public const long INCOMBAT = 0x00000010;
		public const long NOTINCOMBAT = 0x00000020;
		public const long NOTINGROUP = 0x00000040;
		public const long STEALTHED = 0x000000080;
		public const long TARGET = 0x000000100;
	}
}