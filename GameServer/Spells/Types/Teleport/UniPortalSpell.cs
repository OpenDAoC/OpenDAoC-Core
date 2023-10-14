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
	[SpellHandler("UniPortal")]
	public class UniPortalSpell : SpellHandler
	{
		private DbTeleport m_destination;

		public UniPortalSpell(GameLiving caster, Spell spell, SpellLine spellLine, DbTeleport destination)
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
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.UseSlot.CantUseInCombat"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			
			SendEffectAnimation(player, 0, false, 1);

			UniPortalEffect effect = new UniPortalEffect(this, 1000);
			effect.Start(player);

			player.LeaveHouse();
			player.MoveTo((ushort)m_destination.RegionID, m_destination.X, m_destination.Y, m_destination.Z, (ushort)m_destination.Heading);
		}
	}
	
	[SpellHandler("UniPortalKeep")]
	public class UniPortalKeepSpell : SpellHandler
	{
		private DbKeepDoorTeleport m_destination;

		public UniPortalKeepSpell(GameLiving caster, Spell spell, SpellLine spellLine, DbKeepDoorTeleport destination)
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
