using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell handler for speed decreasing spells
	/// </summary>
	[SpellHandler("WarlockSpeedDecrease")]
	public class WarlockSpeedDecreaseSpell : UnbreakableSpeedDecreaseSpell
	{

		private ushort m_playerModel;
		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);

			if(effect.Owner is GamePlayer)
			{
				m_playerModel = effect.Owner.Model;
				if(effect.Owner.Realm == ERealm.Albion)
					effect.Owner.Model = 581;
				else if(effect.Owner.Realm == ERealm.Midgard)
					effect.Owner.Model = 574;
				else if(effect.Owner.Realm == ERealm.Hibernia)
					effect.Owner.Model = 594;

				SendEffectAnimation(effect.Owner, 12126, 0, false, 1);
				//GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.Dying, new DOLEventHandler(OnAttacked));
				//GameEventMgr.AddHandler(effect.Owner, GamePlayerEvent.Linkdeath, new DOLEventHandler(OnAttacked));
				//GameEventMgr.AddHandler(effect.Owner, GamePlayerEvent.Quit, new DOLEventHandler(OnAttacked));
			}
			//GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			//GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttacked));
			if(effect.Owner is GamePlayer)
			{
				effect.Owner.Model = m_playerModel;
				//GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.Dying, new DOLEventHandler(OnAttacked));
				//GameEventMgr.RemoveHandler(effect.Owner, GamePlayerEvent.Linkdeath, new DOLEventHandler(OnAttacked));
				//GameEventMgr.RemoveHandler(effect.Owner, GamePlayerEvent.Quit, new DOLEventHandler(OnAttacked));
			}
			return base.OnEffectExpires(effect, noMessages);
		}

		// constructor
		public WarlockSpeedDecreaseSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
