/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Events;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
	/// <summary>
	/// The Empty Mind Effect
	/// </summary>
	public class TheEmptyMindEffect : TimedEffect, IGameEffect
	{
		private int Value
		{
			get
			{
				return m_value;
			}
		}

		private int m_value = 0;
		/// <summary>
		/// Constructs a new Empty Mind Effect
		/// </summary>
		public TheEmptyMindEffect(int effectiveness, Int32 duration)
			: base(duration)
		{
			m_value = effectiveness;
		}

		/// <summary>
		/// Starts the effect on the living
		/// </summary>
		/// <param name="living"></param>
		public override void Start(GameLiving living)
		{
			base.Start(living);
			m_value = Value;
			m_owner.AbilityBonus[eProperty.Resist_Body] += m_value;
			m_owner.AbilityBonus[eProperty.Resist_Cold] += m_value;
			m_owner.AbilityBonus[eProperty.Resist_Energy] += m_value;
			m_owner.AbilityBonus[eProperty.Resist_Heat] += m_value;
			m_owner.AbilityBonus[eProperty.Resist_Matter] += m_value;
			m_owner.AbilityBonus[eProperty.Resist_Spirit] += m_value;
			if (m_owner is GamePlayer)
				(m_owner as GamePlayer).Out.SendCharResistsUpdate(); 

			foreach (GamePlayer visiblePlayer in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				visiblePlayer.Out.SendSpellEffectAnimation(living, living, 1197, 0, false, 1);
			}
		}

		public override void Stop()
		{
			m_owner.AbilityBonus[eProperty.Resist_Body] -= m_value;
			m_owner.AbilityBonus[eProperty.Resist_Cold] -= m_value;
			m_owner.AbilityBonus[eProperty.Resist_Energy] -= m_value;
			m_owner.AbilityBonus[eProperty.Resist_Heat] -= m_value;
			m_owner.AbilityBonus[eProperty.Resist_Matter] -= m_value;
			m_owner.AbilityBonus[eProperty.Resist_Spirit] -= m_value;
			if (m_owner is GamePlayer)
			{
				(m_owner as GamePlayer).Out.SendCharResistsUpdate();
				(m_owner as GamePlayer).Out.SendMessage("Your clearheaded state leaves you.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}

			base.Stop();
		}

		/// <summary>
		/// Name of the effect
		/// </summary>
		public override string Name
		{
			get
			{
				return "The Empty Mind";
			}
		}

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon
		{
			get { return 3007; }
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var delveInfoList = new List<string>(4);
				delveInfoList.Add(string.Format("Grants the user {0} seconds of increased resistances to all magical damage by the percentage listed.", (m_duration / 1000).ToString()));
				foreach (string str in base.DelveInfo)
					delveInfoList.Add(str);

				return delveInfoList;
			}
		}
	}
}
