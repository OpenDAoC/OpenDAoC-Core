using System;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	/// <summary>
	/// Spell handler for power transfer.
	/// </summary>
	[SpellHandler("PowerTransfer")]
	class PowerTransferSpell : SpellHandler
	{
		/// <summary>
		/// Check if player tries to transfer power to himself.
		/// </summary>
		/// <param name="selectedTarget"></param>
		/// <returns></returns>
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			GamePlayer owner = Owner();
			if (owner == null || selectedTarget == null)
				return false;

			if (selectedTarget == Caster || selectedTarget == owner)
			{
				owner.Out.SendMessage("You cannot transfer power to yourself!",
					EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return false;
			}

			return base.CheckBeginCast(selectedTarget);
		}

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null) return;
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			// Calculate the amount of power to transfer from the owner.
			// TODO: Effectiveness plays a part here.

			GamePlayer owner = Owner();
			if (owner == null)
				return;

			int powerTransfer = (int)Math.Min(Spell.Value, owner.Mana);
			int powerDrained = -owner.ChangeMana(owner, EPowerChangeType.Spell, -powerTransfer);

			if (powerDrained <= 0)
				return;

			int powerHealed = target.ChangeMana(owner, EPowerChangeType.Spell, powerDrained);

			if (powerHealed <= 0)
			{
				SendEffectAnimation(target, 0, false, 0);
				owner.Out.SendMessage(String.Format("{0} is at full power already!",
					target.Name), EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
			}
			else
			{
				SendEffectAnimation(target, 0, false, 1);
				owner.Out.SendMessage(String.Format("You transfer {0} power to {1}!",
					powerHealed, target.Name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);

				if (target is GamePlayer)
					(target as GamePlayer).Out.SendMessage(String.Format("{0} transfers {1} power to you!",
						owner.Name, powerHealed), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
			}
		}

		/// <summary>
		/// Returns a reference to the shade.
		/// </summary>
		/// <returns></returns>
		protected virtual GamePlayer Owner()
		{
			if (Caster is GamePlayer)
				return Caster as GamePlayer;
			
			return null;
		}

		/// <summary>
        /// Create a new handler for the power transfer spell.
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="line"></param>
		public PowerTransferSpell(GameLiving caster, Spell spell, SpellLine line) 
            : base(caster, spell, line) { }
	}
}
