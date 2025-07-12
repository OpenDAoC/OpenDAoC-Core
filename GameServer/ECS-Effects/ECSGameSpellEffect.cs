using System;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;

namespace DOL.GS
{
    /// <summary>
    /// Spell-Based Effect
    /// </summary>
    public class ECSGameSpellEffect : ECSGameEffect, IConcentrationEffect
    {
        public new ISpellHandler SpellHandler;
        string IConcentrationEffect.Name => Name;
        ushort IConcentrationEffect.Icon => Icon;
        byte IConcentrationEffect.Concentration => SpellHandler.Spell.Concentration;
        public override ushort Icon => SpellHandler.Spell.Icon;
        public override string Name => SpellHandler.Spell.Name;
        public override bool HasPositiveEffect => SpellHandler != null && SpellHandler.HasPositiveEffect;
        public bool IsAllowedToPulse => NextTick > 0 && PulseFreq > 0;

        public ECSGameSpellEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            SpellHandler = initParams.SpellHandler;
            Spell spell = SpellHandler.Spell;
            EffectType = EffectService.GetEffectFromSpell(SpellHandler.Spell);
            PulseFreq = spell.Frequency;

            if (spell.SpellType is eSpellType.SpeedDecrease or eSpellType.StyleSpeedDecrease or eSpellType.UnbreakableSpeedDecrease)
            {
                PulseFreq = 250;
                NextTick = 1 + Duration / 2 + StartTick + PulseFreq;

                if (!spell.Name.Equals("Prevent Flight", StringComparison.OrdinalIgnoreCase) && !spell.IsFocus)
                    TriggersImmunity = true;
            }
            else if (spell.IsConcentration)
            {
                PulseFreq = 2500;
                NextTick = StartTick + PulseFreq;
            }

            // These classes start their effects themselves.
            if (this is not ECSImmunityEffect and not ECSPulseEffect and not BleedECSEffect)
                Start();
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
            // Only handle players. NPCs have their own immunity logic.
            if (!TriggersImmunity || OwnerPlayer == null)
                return;

            // Summoned pets don't give stun immunities (maybe tweak their spells instead?)
            if (EffectType is eEffect.Stun && SpellHandler.Caster is GameSummonedPet)
                return;

            if (SpellHandler is UnresistableStunSpellHandler)
                return;

            new ECSImmunityEffect(Owner, SpellHandler, ImmunityDuration, (int) PulseFreq, Effectiveness);
        }

        public override DbPlayerXEffect GetSavedEffect()
        {
            if (SpellHandler?.Spell == null)
                return null;

            DbPlayerXEffect eff = new()
            {
                Var1 = SpellHandler.Spell.ID,
                Var2 = Effectiveness,
                Var3 = (int) SpellHandler.Spell.Value,
                IsHandler = true,
                SpellLine = SpellHandler.SpellLine.KeyName
            };

            if (Duration > 0)
                eff.Duration = (int) (ExpireTick - GameLoop.GameLoopTime);

            return eff;
        }

        protected static IPropertyIndexer GetPropertyIndexer(GameLiving target, eBuffBonusCategory buffBonusCategory)
        {
            return buffBonusCategory switch
            {
                eBuffBonusCategory.BaseBuff => target.BaseBuffBonusCategory,
                eBuffBonusCategory.SpecBuff => target.SpecBuffBonusCategory,
                eBuffBonusCategory.AbilityBuff => target.AbilityBonus,
                eBuffBonusCategory.OtherBuff => target.OtherBonus,
                eBuffBonusCategory.Debuff => target.DebuffCategory,
                eBuffBonusCategory.SpecDebuff => target.SpecDebuffCategory,
                _ => null,
            };
        }

        protected static void ApplyBonus(GameLiving owner, eBuffBonusCategory bonusCategory, eProperty property, double value, double effectiveness, bool isSubtracted)
        {
            if (property is eProperty.Undefined)
                return;

            if (isSubtracted)
                value = -value;

            int effectiveValue = (int) (value * effectiveness);

            if (owner is GamePlayer player && player.UseDetailedCombatLog)
                player.Out.SendMessage($"BonusCategory: {bonusCategory} | Property: {property}\nValue: {value:0.##} | Effectiveness: {effectiveness:0.##} | EffectiveValue: {effectiveValue}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

            GetPropertyIndexer(owner, bonusCategory)[property] += effectiveValue;
        }
    }
}
