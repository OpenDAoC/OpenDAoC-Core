using DOL.GS.Effects;
using DOL.GS.Spells;

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
    }
}