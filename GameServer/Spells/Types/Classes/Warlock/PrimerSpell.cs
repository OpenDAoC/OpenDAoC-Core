using System;
using Core.Events;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
    public class PrimerSpell : SpellHandler
	{
		/// <summary>
		/// Cast Powerless
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			
			base.FinishSpellCast(target);
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			return new GameSpellEffect(this, Spell.Duration, 0, effectiveness);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{			
			GameEventMgr.AddHandler(effect.Owner, GamePlayerEvent.Moving, new CoreEventHandler(OnMove));
			SendEffectAnimation(effect.Owner, 0, false, 1);			
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if(effect.Owner is GamePlayer && !noMessages)
				((GamePlayer)effect.Owner).Out.SendMessage("You modification spell effect has expired.", EChatType.CT_SpellExpires, EChatLoc.CL_SystemWindow);

			GameEventMgr.RemoveHandler(effect.Owner, GamePlayerEvent.Moving, new CoreEventHandler(OnMove));

			return base.OnEffectExpires (effect, false);
		}

	
		/// <summary>
		/// Handles attacks on player/by player
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		private void OnMove(CoreEvent e, object sender, EventArgs arguments)
		{
			GameLiving living = sender as GameLiving;
			if (living == null) return;
			if(living.IsMoving)
			{
				// remove speed buff if in combat
				GameSpellEffect effect = SpellHandler.FindEffectOnTarget(living, this);
				if (effect != null)
				{
					effect.Cancel(false);
					((GamePlayer)living).Out.SendMessage("You move and break your modification spell.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				}
			}
		}


		// constructor
		public PrimerSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
