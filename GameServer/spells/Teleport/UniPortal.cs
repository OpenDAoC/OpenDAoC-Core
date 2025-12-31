using DOL.Database;
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
			if (target is not GamePlayer player)
				return;

			if (player.InCombat || GameRelic.IsPlayerCarryingRelic(player))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.UseSlot.CantUseInCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			// Won't do anything since the player is moved instantly.
			// SendEffectAnimation(player, 0, false, 1);
			player.LeaveHouse();
			player.MoveTo((ushort) m_destination.RegionID, m_destination.X, m_destination.Y, m_destination.Z, (ushort) m_destination.Heading);
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
			if (target is not GamePlayer player)
				return;

			/*if (player.IsAlive && !player.IsStunned && !player.IsMezzed)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GamePlayer.UseSlot.CantUseInCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}*/

			// Won't do anything since the player is moved instantly.
			// SendEffectAnimation(player, 0, false, 1);
			player.LeaveHouse();
			player.MoveTo(m_destination.Region, m_destination.X, m_destination.Y, m_destination.Z, m_destination.Heading);
		}
	}
}
