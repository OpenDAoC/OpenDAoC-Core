using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Heal Over Time spell handler
	/// </summary>
	[SpellHandler(eSpellType.HealOverTime)]
	public class HoTSpellHandler : SpellHandler
	{
		public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			return new HealOverTimeECSGameEffect(initParams);
		}

		/// <summary>
		/// Execute heal over time spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			return new GameSpellEffect(this, Spell.Duration, Spell.Frequency, effectiveness);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{			
			//SendEffectAnimation(effect.Owner, 0, false, 1);
			////"{0} seems calm and healthy."
			//Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_Spell, effect.Owner);
		}

		public override void OnEffectPulse(GameSpellEffect effect)
		{
			base.OnEffectPulse(effect);
			OnDirectEffect(effect.Owner);
		}

		public override void OnDirectEffect(GameLiving target)
		{
			if (target.ObjectState != GameObject.eObjectState.Active) return;
			if (target.IsAlive == false) return;

			base.OnDirectEffect(target);
			double heal = Spell.Value * CalculateBuffDebuffEffectiveness();
			
			if(target.Health < target.MaxHealth)
            {
				target.Health += (int)heal;
				if (target is NecromancerPet && Caster.Equals(target))
					MessageToLiving((target as NecromancerPet).Owner, "Your " + target.GetName(0, false) + " is healed for " + heal + " hit points!", eChatType.CT_Spell);
				else
					MessageToLiving(target, "You are healed by " + m_caster.GetName(0, false) + " for " + heal + " hit points.", eChatType.CT_Spell);
			}
            else
            {
				MessageToLiving(target, "You are full health.", eChatType.CT_SpellResisted);
            }

			//"You feel calm and healthy."
			MessageToLiving(target, Spell.Message1, eChatType.CT_Spell);
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
			base.OnEffectExpires(effect, noMessages);
			if (!noMessages) {
				//"Your meditative state fades."
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				//"{0}'s meditative state fades."
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}
			return 0;
		}


		// constructor
		public HoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
