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
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Events;

namespace DOL.GS.Effects
{
	/// <summary>
	/// OF Reflex Attack Effect
	/// </summary>
	public class AtlasOF_ReflexAttackEffect : TimedEffect, IGameEffect
	{
		public AtlasOF_ReflexAttackEffect(Int32 duration) : base(duration) { }

		public override void Start(GameLiving living)
		{
			base.Start(living);

			foreach (GamePlayer t_player in living.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (t_player == living && living is GamePlayer)
                {
                    (living as GamePlayer).Out.SendMessage("You begin automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
                else
                {
                    t_player.Out.SendMessage(living.Name + " starts automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
            }
        }

        public override void Stop()
		{
            foreach (GamePlayer t_player in m_owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (t_player == m_owner && m_owner is GamePlayer)
                {
                    (m_owner as GamePlayer).Out.SendMessage("You stop automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
                else
                {
                    t_player.Out.SendMessage(m_owner.Name + " stops automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
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
				return "Reflex Attack";
			}
		}

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon
		{
			get { return 3011; }
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var delveInfoList = new List<string>(4);
				delveInfoList.Add(string.Format("For {0} seconds, you will automatically counter-attack any melee attack immediately, with no timer, There is no limit to the number of attackers you can potentially Reflex Attack - any player or monster who attacks you will receive an immediate counter-attack.", (m_duration / 1000).ToString()));
				foreach (string str in base.DelveInfo)
					delveInfoList.Add(str);

				return delveInfoList;
			}
		}
	}
}
