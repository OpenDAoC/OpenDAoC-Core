using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.Spells;
using Core.GS.Styles;
using Core.GS.World;

namespace Core.GS.ECS;

public class DamageOverTimeEcsSpellEffect : EcsGameSpellEffect
{
    public DamageOverTimeEcsSpellEffect(EcsGameEffectInitParams initParams)
        : base(initParams) 
    {
        NextTick = GameLoopMgr.GameLoopTime;
    }

    public override void OnStartEffect()
    {
        // Remove stealth on first application since the code that normally handles removing stealth on
        // attack ignores DoT damage, since only the first tick of a DoT should remove stealth.            
        if (OwnerPlayer != null && !OwnerPlayer.effectListComponent.ContainsEffectForEffectType(EEffect.Vanish))
            OwnerPlayer.Stealth(false);
        
        // "Searing pain fills your mind!"
        // "{0} is wracked with pain!"
        //OnEffectStartsMsg(Owner, true, false, true);
    }

    public override void OnStopEffect()
    {
        if (EffectType == EEffect.Bleed && !Owner.effectListComponent.ContainsEffectForEffectType(EEffect.Bleed))
            Owner.TempProperties.RemoveProperty(StyleBleedingEffect.BLEED_VALUE_PROPERTY);
        
        // "Your mental agony fades."
        // "{0}'s mental agony fades."
        OnEffectExpiresMsg(Owner, true, false, true);

    }

    public override void OnEffectPulse()
    {
        if (Owner.IsAlive == false)
        {
            EffectService.RequestImmediateCancelEffect(this);
        }

        if (Owner.IsAlive)
        {
            if (SpellHandler is DamageOverTimeSpell handler)
            {
                if (OwnerPlayer != null)
                {
                    // "Searing pain fills your mind!"
                    // "{0} is wracked with pain!"
                    OnEffectStartsMsg(Owner, true, false, true);
                }

                if (handler.Caster.effectListComponent.ContainsEffectForEffectType(EEffect.Viper) && SpellHandler.Spell.IsPoison)
                {
                    Effectiveness *= 2;
                    handler.OnDirectEffect(Owner);
                    Effectiveness /= 2;
                }
                else
                    handler.OnDirectEffect(Owner);
            }
            else if (SpellHandler is StyleBleedingEffect bleedHandler)
            {

                if (Owner.effectListComponent.ContainsEffectForEffectType(EEffect.Bleed)
                    && Owner.TempProperties.GetProperty<int>(StyleBleedingEffect.BLEED_VALUE_PROPERTY) > bleedHandler.Spell.Damage)
                {
                    if (OwnerPlayer != null)
                        bleedHandler.MessageToCaster("A stronger bleed effect already exists on your target.", EChatType.CT_SpellResisted);
                    EffectService.RequestCancelEffect(this);
                    return;
                }

                if (StartTick + PulseFreq > GameLoopMgr.GameLoopTime && Owner.TempProperties.GetProperty<int>(StyleBleedingEffect.BLEED_VALUE_PROPERTY) < bleedHandler.Spell.Damage)
                {
                    Owner.TempProperties.SetProperty(StyleBleedingEffect.BLEED_VALUE_PROPERTY, (int)bleedHandler.Spell.Damage); 
                }

                if (OwnerPlayer != null)
                {
                    bleedHandler.MessageToLiving(Owner, bleedHandler.Spell.Message1, EChatType.CT_YouWereHit);
                    MessageUtil.SystemToArea(Owner, Util.MakeSentence(bleedHandler.Spell.Message2, Owner.GetName(0, false)), EChatType.CT_YouHit, Owner);
                }

                int bleedValue = Owner.TempProperties.GetProperty<int>(StyleBleedingEffect.BLEED_VALUE_PROPERTY);

                Effectiveness = 1;
                AttackData ad = bleedHandler.CalculateDamageToTarget(Owner);
                bleedHandler.SendDamageMessages(ad);

                // attacker must be null, attack result is 0x0A
                foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
                }
                // send animation before dealing damage else dead livings show no animation
                ad.Target.OnAttackedByEnemy(ad);
                ad.Attacker.DealDamage(ad);

                if(bleedValue > 1)
                    bleedValue--;

                if (!Owner.IsAlive)
                {
                    EffectService.RequestImmediateCancelEffect(this);
                }
                else Owner.TempProperties.SetProperty(StyleBleedingEffect.BLEED_VALUE_PROPERTY, bleedValue);
            }

            if (Owner.Realm == 0 || SpellHandler.Caster.Realm == 0)
                Owner.LastAttackTickPvE = GameLoopMgr.GameLoopTime;
            else
                Owner.LastAttackTickPvP = GameLoopMgr.GameLoopTime;
        }

        if (LastTick == 0)
        {
            LastTick = GameLoopMgr.GameLoopTime;
            NextTick = LastTick + PulseFreq;
        }
        else
        {
            LastTick += PulseFreq;
            NextTick = LastTick + PulseFreq;
        }
        
        if(SpellHandler.Caster is GameSummonedPet)
            Owner.StartInterruptTimer(SpellHandler.Caster.SpellInterruptDuration, EAttackType.Spell, SpellHandler.Caster);
            
    }
}