using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	[SpellHandler("HpPwrEndRegen")]
	public class HpPwrEndRegenSpell : SpellHandler
	{
		/// <summary>
		/// Execute heal over time spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			//m_caster.Mana -= CalculateNeededPower(target);
			base.FinishSpellCast(target);
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			return new GameSpellEffect(this, Spell.Duration, Spell.Frequency, effectiveness);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{			
			SendEffectAnimation(effect.Owner, 0, false, 1);
			//"{0} seems calm and healthy."
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), EChatType.CT_Spell, effect.Owner);
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

            int healthtick = (int)(target.MaxHealth * 0.05);
            int manatick = (int)(target.MaxMana * 0.05);
            int endutick = (int)(target.MaxEndurance * 0.05);

            int modendu = target.MaxEndurance - target.Endurance;
            if (modendu > endutick)
                modendu = endutick;
            target.Endurance += modendu;
            int modheal = target.MaxHealth - target.Health;
            if (modheal > healthtick)
                modheal = healthtick;
            target.Health += modheal;
            int modmana = target.MaxMana - target.Mana;
            if (modmana > manatick)
                modmana = manatick;
            target.Mana += modmana;
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
				MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
				//"{0}'s meditative state fades."
				MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), EChatType.CT_SpellExpires, effect.Owner);
			}
			return 0;
		}

		// constructor
        public HpPwrEndRegenSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}