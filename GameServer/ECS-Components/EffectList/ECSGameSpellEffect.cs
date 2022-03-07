using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    
    /// <summary>
    /// Spell-Based Effect
    /// </summary>
    public class ECSGameSpellEffect : ECSGameEffect, IConcentrationEffect
    {
        public ISpellHandler SpellHandler;
        string IConcentrationEffect.Name => Name;
        ushort IConcentrationEffect.Icon => Icon;
        byte IConcentrationEffect.Concentration => SpellHandler.Spell.Concentration;

        public override ushort Icon { get { return SpellHandler.Spell.Icon; } }
        public override string Name { get { return SpellHandler.Spell.Name; } }
        public override bool HasPositiveEffect { get { return SpellHandler == null ? false : SpellHandler.HasPositiveEffect; } }

        public ECSGameSpellEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            SpellHandler = initParams.SpellHandler;
            //SpellHandler = SpellHandler; // this is the base ECSGameEffect handler , temp during conversion into different classes
            EffectType = MapSpellEffect();
            PulseFreq = SpellHandler.Spell != null ? SpellHandler.Spell.Frequency : 0;

            if (SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease || SpellHandler.Spell.SpellType == (byte)eSpellType.UnbreakableSpeedDecrease)
            {
                TickInterval = 650;
                NextTick = 1 + (Duration >> 1) + (int)StartTick;
                TriggersImmunity = true;
            }
            else if (SpellHandler.Spell.IsConcentration)
            {
                NextTick = StartTick;
                // 60 seconds taken from PropertyChangingSpell
                // Not sure if this is correct
                PulseFreq = 650;
            }

            if (this is not ECSImmunityEffect && this is not ECSPulseEffect)
                EffectService.RequestStartEffect(this);
        }

        private eEffect MapSpellEffect()
        {
            if (SpellHandler.SpellLine.IsBaseLine)
            {
                SpellHandler.Spell.IsSpec = false;
            }
            else
            {
                SpellHandler.Spell.IsSpec = true;
            }

            return EffectService.GetEffectFromSpell(SpellHandler.Spell);
        }

        public override bool IsConcentrationEffect()
        {
            return SpellHandler.Spell.IsConcentration;
        }

        public override bool ShouldBeAddedToConcentrationList()
        {
            return SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public override bool ShouldBeRemovedFromConcentrationList()
        {
            return SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public override void TryApplyImmunity()
        {
            if (TriggersImmunity)
            {
                if (OwnerPlayer != null)
                {
                    if (EffectType == eEffect.Stun && SpellHandler.Caster is GamePet)
                        return;

                    new ECSImmunityEffect(Owner, SpellHandler, ImmunityDuration, (int)PulseFreq, Effectiveness, Icon);
                }
                else if (Owner is GameNPC)
                {
                    if (EffectType == eEffect.Stun)
                    {
                        NPCECSStunImmunityEffect npcImmune = (NPCECSStunImmunityEffect)EffectListService.GetEffectOnTarget(Owner, eEffect.NPCStunImmunity);
                        if (npcImmune is null)
                            new NPCECSStunImmunityEffect(new ECSGameEffectInitParams(Owner, ImmunityDuration, Effectiveness));
                    }
                    else if (EffectType == eEffect.Mez)
                    {
                        NPCECSMezImmunityEffect npcImmune = (NPCECSMezImmunityEffect)EffectListService.GetEffectOnTarget(Owner, eEffect.NPCMezImmunity);
                        if (npcImmune is null)
                            new NPCECSMezImmunityEffect(new ECSGameEffectInitParams(Owner, ImmunityDuration, Effectiveness));
                    }
                }
            }
        }
        
        /// <summary>
        /// Specifies the entity casting the spell
        /// </summary>
        public GameLiving Caster { get; set; }
        
        /// <summary>
		/// Sends messages when a spell effect affects the target. These values are set from the 'spell' table in the database.
		/// </summary>
		/// <param name="target">The target of the spell or spell effect.</param>
        /// <param name="msgTarget">When set to 'true', the system sends a first-person spell message to the target.</param>
		/// <param name="msgSelf">When set to 'true', the system sends a third-person spell message to the caster (ignoring proximity to the target).</param>
		/// <param name="msgArea">When set to 'true', the system sends a third-person spell message to all players near the target.</param>
		public virtual void OnEffectStartsMsg(GameLiving target, bool msgTarget, bool msgSelf, bool msgArea)
		{
			// If the target variable is at the start of the string, capitalize their name or article
			bool upperCase = SpellHandler.Spell.Message2.StartsWith("{0}");

			// Sends a first-person message directly to the caster's target, if they are a player.
			if (msgTarget && target is GamePlayer)
				// "You feel more dexterous!"
				((SpellHandler) SpellHandler).MessageToLiving(target, SpellHandler.Spell.Message1, eChatType.CT_Spell);
			
			// Sends a third-person message directly to the caster to indicate the spell has taken effect.
			if (msgSelf && target is GamePlayer && Caster != target)
			{
				if (Caster == target) return;
				
				// "{0} looks more agile!"
				((SpellHandler) SpellHandler).MessageToCaster(eChatType.CT_Spell, SpellHandler.Spell.Message2, target.GetName(0, upperCase));
			}
			
			// Sends a third-person message to all players surrounding the target
			if (msgArea)
			{
					// "{0} looks more agile!"
					Message.SystemToArea(target, Util.MakeSentence(SpellHandler.Spell.Message2, target.GetName(0, upperCase)), eChatType.CT_Spell, target);
			}
		}
        
		/// <summary>
		/// Sends spell messages when a spell effect ends or expires on the target.
		/// </summary>
		/// <param name="target">The target of the spell or spell effect.</param>
		/// <param name="msgTarget">When set to 'true', the system sends a first-person spell message to the target.</param>
		/// <param name="msgSelf">When set to 'true', the system sends a third-person spell message to the caster (ignoring proximity to the target).</param>
		/// <param name="msgArea">When set to 'true', the system sends a third-person spell message to all players near the target.</param>
		public void OnEffectExpiresMsg(GameLiving target, bool msgTarget, bool msgSelf, bool msgArea)
		{
			// If the target variable is at the start of the string, capitalize their name or article
			bool upperCase = SpellHandler.Spell.Message4.StartsWith("{0}");

			// Sends no messages.
			if (msgTarget == false && msgSelf == false && msgArea == false)
				return;
			
			// Sends a first-person message directly to the caster's target, if they are a player.
			if (msgTarget && target is GamePlayer)
				// "Your agility returns to normal."
				((SpellHandler) SpellHandler).MessageToLiving(target, SpellHandler.Spell.Message3, eChatType.CT_Spell);
			if (msgSelf && target is GamePlayer)
			{
				if (Caster == target) return;
				
				// "{0}'s enhanced agility fades."
				((SpellHandler) SpellHandler).MessageToCaster(eChatType.CT_Spell, SpellHandler.Spell.Message4, Owner.GetName(0, upperCase));
			}
			if (msgArea)
			{
				// "{0}'s enhanced agility fades."
				Message.SystemToArea(target, Util.MakeSentence(SpellHandler.Spell.Message4, target.GetName(0, upperCase)), eChatType.CT_System, target);
			}
		}

    }
}