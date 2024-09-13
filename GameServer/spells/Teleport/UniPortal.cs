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

using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
	/// <summary>
	/// The spell used by classic teleporters.
	/// </summary>
	/// <author>Aredhel</author>
	[SpellHandler(eSpellType.UniPortal)]
	public class UniPortal : SpellHandler
	{
		private DbTeleport m_destination;

		public UniPortal(GameLiving caster, Spell spell, SpellLine spellLine, DbTeleport destination)
			: base(caster, spell, spellLine) 
		{
			m_destination = destination;
		}

		/// <summary>
		/// Whether this spell can be cast on the selected target at all.
		/// </summary>
		/// <param name="selectedTarget"></param>
		/// <returns></returns>
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget))
				return false;
			return (selectedTarget is GamePlayer);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			GamePlayer player = target as GamePlayer;
			if (player == null)
				return;
			
			if (player.InCombat || GameRelic.IsPlayerCarryingRelic(player))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.UseSlot.CantUseInCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			
			SendEffectAnimation(player, 0, false, 1);

			UniPortalEffect effect = new UniPortalEffect(this, 1000);
			effect.Start(player);

			player.LeaveHouse();
			player.MoveTo((ushort)m_destination.RegionID, m_destination.X, m_destination.Y, m_destination.Z, (ushort)m_destination.Heading);
		}
	}
	
	[SpellHandler(eSpellType.UniPortalKeep)]
	public class UniPortalKeep : SpellHandler
	{
		private DbKeepDoorTeleport m_destination;

		public UniPortalKeep(GameLiving caster, Spell spell, SpellLine spellLine, DbKeepDoorTeleport destination)
			: base(caster, spell, spellLine)
		{
			m_destination = destination;
		}

		/// <summary>
		/// Whether this spell can be cast on the selected target at all.
		/// </summary>
		/// <param name="selectedTarget"></param>
		/// <returns></returns>
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget))
				return false;
			return (selectedTarget is GamePlayer);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			GamePlayer player = target as GamePlayer;
			if (player == null)
				return;

			/*
			if (player.IsAlive && !player.IsStunned && !player.IsMezzed)
			{
			    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GamePlayer.UseSlot.CantUseInCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			    return;
			}
			*/
			SendEffectAnimation(player, 0, false, 1);

			UniPortalEffect effect = new UniPortalEffect(this, 1500);
			effect.Start(player);

			player.LeaveHouse();
			player.MoveTo((ushort)m_destination.Region, m_destination.X, m_destination.Y, m_destination.Z, (ushort)m_destination.Heading);
		}
	}
}
