using System.Collections.Generic;
using System.Linq;
using DOL.GS.PlayerClass;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class StatDebuffECSEffect : ECSGameSpellEffect
    {
        private bool _isForcedToSpecDebuff;

        public StatDebuffECSEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            base.OnStartEffect();

            if (Owner is GamePlayer player)
                TryDebuffInterrupt(SpellHandler.Spell, player, SpellHandler.Caster);

            if (Owner.effectListComponent.Effects.ContainsKey(EffectType))
            {
                List<ECSGameSpellEffect> effects = Owner.effectListComponent.GetSpellEffects(EffectType);

                foreach (ECSGameSpellEffect e in effects)
                {
                    if (e.SpellHandler.Spell.ID == SpellHandler.Spell.ID && IsBuffActive)
                        return;
                }
            }

            // Force Champion's stat debuffs to be applied as spec debuffs (see `StatCalculator`).
            if (SpellHandler.Caster is GamePlayer playerCaster &&
                playerCaster.CharacterClass is ClassChampion &&
                SpellHandler.SpellLine.KeyName is GlobalSpellsLines.Valor &&
                (EffectService.GetPlayerUpdateFromEffect(EffectType) & EffectService.PlayerUpdate.STATS) != 0)
            {
                _isForcedToSpecDebuff = true;
            }

            if (EffectType is eEffect.MovementSpeedDebuff)
            {
                IEnumerable<ECSGameSpellEffect> speedDebuffs = Owner.effectListComponent.GetSpellEffects(eEffect.MovementSpeedDebuff).Where(x => x.SpellHandler.Spell.ID != SpellHandler.Spell.ID);

                if (speedDebuffs.Any(x => x.SpellHandler.Spell.Value > SpellHandler.Spell.Value))
                    return;

                foreach (ECSGameSpellEffect effect in speedDebuffs)
                    EffectService.RequestDisableEffect(effect);

                double effectiveValue = SpellHandler.Spell.Value * Effectiveness;
                Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, EffectType, 1.0 - effectiveValue * 0.01);
                Owner.OnMaxSpeedChange();
            }
            else
            {
                if (SpellHandler is not PropertyChangingSpell propertyChangingSpell)
                    return;

                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, _isForcedToSpecDebuff ? eBuffBonusCategory.SpecDebuff : propertyChangingSpell.BonusCategory1, property, SpellHandler.Spell.Value, Effectiveness, true);
            }

            // "Your agility is suppressed!"
            // "{0} seems uncoordinated!"
            OnEffectStartsMsg(Owner, true, true, true);
        }

        public override void OnStopEffect()
        {
            base.OnStartEffect();

            if (EffectType is eEffect.MovementSpeedDebuff)
            {
                ECSGameSpellEffect speedDebuff = Owner.effectListComponent.GetBestDisabledSpellEffect(eEffect.MovementSpeedDebuff);

                if (speedDebuff != null)
                    EffectService.RequestEnableEffect(speedDebuff);

                Owner.BuffBonusMultCategory1.Remove((int) eProperty.MaxSpeed, EffectType);
                Owner.OnMaxSpeedChange();
            }
            else
            {
                if (SpellHandler is not PropertyChangingSpell propertyChangingSpell)
                    return;

                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, _isForcedToSpecDebuff ? eBuffBonusCategory.SpecDebuff : propertyChangingSpell.BonusCategory1, property, SpellHandler.Spell.Value, Effectiveness, false);
            }

            // Let's not bother checking the effect type and simply attempt to start every regeneration timer instead.
            Owner.StartHealthRegeneration();
            Owner.StartEnduranceRegeneration();
            Owner.StartPowerRegeneration();

            // "Your coordination returns."
            // "{0}'s coordination returns."
            OnEffectExpiresMsg(Owner, true, false, true);
            IsBuffActive = false;
        }

        public static void TryDebuffInterrupt(Spell spell, GamePlayer player, GameLiving caster)
        {
            if (spell.ID is not 10031 and //BD insta debuffs
                not 10032 and
                not 10033 and
                not 9631 and //reaver pbae insta melee damage reductions
                not 9632 and
                not 9633 and
                not 9634 and
                not 9635 and
                not 9636 and
                not 9637 and
                not 9601 and //reaver pbae insta abs reductions
                not 9602 and
                not 9603 and
                not 9604 and
                not 9605 and
                not 9606)
                return;

            if (player != null)
            {
                player.StopCurrentSpellcast();
                player.StartInterruptTimer(player.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
            }
        }
    }
}
