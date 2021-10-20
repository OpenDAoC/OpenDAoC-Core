using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
    
    /// <summary>
    /// Spell-Based Effect
    /// </summary>
    public class ECSGameSpellEffect : ECSGameEffect, IConcentrationEffect
    {
        public ISpellHandler spellHandler;
        string IConcentrationEffect.Name => Name;
        ushort IConcentrationEffect.Icon => Icon;
        byte IConcentrationEffect.Concentration => spellHandler.Spell.Concentration;

        public override ushort Icon { get { return spellHandler.Spell.Icon; } }
        public override string Name { get { return spellHandler.Spell.Name; } }
        public override bool HasPositiveEffect { get { return spellHandler == null ? false : spellHandler.HasPositiveEffect; } }

        public ECSGameSpellEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = MapSpellEffect();

            spellHandler = initParams.SpellHandler;
            SpellHandler = spellHandler; // this is the base ECSGameEffect handler , temp during conversion into different classes
            PulseFreq = spellHandler.Spell != null ? spellHandler.Spell.Frequency : 0;

            if (spellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
            {
                TickInterval = 650;
                NextTick = 1 + (Duration >> 1) + (int)StartTick;
            }
            else if (spellHandler.Spell.IsConcentration)
            {
                NextTick = StartTick;
                // 60 seconds taken from PropertyChangingSpell
                // Not sure if this is correct
                PulseFreq = 650;
            }

            EntityManager.AddEffect(this);
        }

        private eEffect MapSpellEffect()
        {
            if (spellHandler.SpellLine.IsBaseLine)
            {
                spellHandler.Spell.IsSpec = false;
            }
            else
            {
                spellHandler.Spell.IsSpec = true;
            }

            return EffectService.GetEffectFromSpell(spellHandler.Spell);
        }

        public override bool IsConcentrationEffect()
        {
            return spellHandler.Spell.IsConcentration;
        }

        public override bool ShouldBeAddedToConcentrationList()
        {
            return spellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public override bool ShouldBeRemovedFromConcentrationList()
        {
            return spellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public override void TryApplyImmunity()
        {
            if (TriggersImmunity && OwnerPlayer != null)
            {
                new ECSImmunityEffect(Owner, spellHandler, ImmunityDuration, (int)PulseFreq, Effectiveness, Icon);
            }
        }
    }
}