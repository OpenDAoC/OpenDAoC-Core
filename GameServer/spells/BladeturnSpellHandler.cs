using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Bladeturn spellhandler
	/// </summary>
	[SpellHandlerAttribute("Bladeturn")]
	public class BladeturnSpellHandler : SpellHandler
	{
        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new BladeturnECSGameEffect(initParams);
        }

        /// <summary>
        /// Execute buff spell
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			//base.OnEffectStart(effect);
			//eChatType toLiving = (Spell.Pulse == 0) ? eChatType.CT_Spell : eChatType.CT_SpellPulse;
			//eChatType toOther = (Spell.Pulse == 0) ? eChatType.CT_System : eChatType.CT_SpellPulse;
			//MessageToLiving(effect.Owner, Spell.Message1, toLiving);
			//Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), toOther, effect.Owner);
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
			//if (!noMessages && Spell.Pulse == 0) 
			//{
			//	MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
			//	Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			//}
			return 0;
		}

        /// <summary>
        /// Saves the effect on player exit
        /// </summary>        
        public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            DbPlayerXEffect eff = new DbPlayerXEffect();
            eff.Var1 = Spell.ID;
            eff.Duration = e.RemainingTime;
            eff.IsHandler = true;
            eff.Var2 = (int)(Spell.Value * e.Effectiveness);
            eff.SpellLine = SpellLine.KeyName;
            return eff;
        }

        /// <summary>
        /// Send messages when a restored effect expires
        /// </summary>        
        public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
        {            
            if (!noMessages && Spell.Pulse == 0)
            {
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
                Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
            }
            return 0;
        }

        // constructor
        public BladeturnSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
