﻿using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class DamageOverTimeEcsEffect : EcsGameSpellEffect
    {
        public DamageOverTimeEcsEffect(ECSGameEffectInitParams initParams)
            : base(initParams) 
        {
            NextTick = GameLoop.GameLoopTime;
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
                Owner.TempProperties.removeProperty(StyleBleedingHandler.BLEED_VALUE_PROPERTY);
            
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
                if (SpellHandler is DotHandler handler)
                {
                    if (OwnerPlayer != null)
                    {
                        // "Searing pain fills your mind!"
                        // "{0} is wracked with pain!"
                        OnEffectStartsMsg(Owner, true, false, true);
                    }

                    double postRAEffectiveness = Effectiveness;
                    if (handler.Caster.effectListComponent.ContainsEffectForEffectType(EEffect.Viper) && SpellHandler.Spell.IsPoison)
                        postRAEffectiveness *= 2;
                    
                    handler.OnDirectEffect(Owner, postRAEffectiveness);
                }
                else if (SpellHandler is StyleBleedingHandler bleedHandler)
                {

                    if (Owner.effectListComponent.ContainsEffectForEffectType(EEffect.Bleed)
                        && Owner.TempProperties.getProperty<int>(StyleBleedingHandler.BLEED_VALUE_PROPERTY) > bleedHandler.Spell.Damage)
                    {
                        if (OwnerPlayer != null)
                            bleedHandler.MessageToCaster("A stronger bleed effect already exists on your target.", EChatType.CT_SpellResisted);
                        EffectService.RequestCancelEffect(this);
                        return;
                    }

                    if (StartTick + PulseFreq > GameLoop.GameLoopTime && Owner.TempProperties.getProperty<int>(StyleBleedingHandler.BLEED_VALUE_PROPERTY) < bleedHandler.Spell.Damage)
                    {
                        Owner.TempProperties.setProperty(StyleBleedingHandler.BLEED_VALUE_PROPERTY, (int)bleedHandler.Spell.Damage); 
                    }

                    if (OwnerPlayer != null)
                    {
                        bleedHandler.MessageToLiving(Owner, bleedHandler.Spell.Message1, EChatType.CT_YouWereHit);
                        Message.SystemToArea(Owner, UtilCollection.MakeSentence(bleedHandler.Spell.Message2, Owner.GetName(0, false)), EChatType.CT_YouHit, Owner);
                    }

                    int bleedValue = Owner.TempProperties.getProperty<int>(StyleBleedingHandler.BLEED_VALUE_PROPERTY);

                    AttackData ad = bleedHandler.CalculateDamageToTarget(Owner, 1.0);
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
                    else Owner.TempProperties.setProperty(StyleBleedingHandler.BLEED_VALUE_PROPERTY, bleedValue);
                }

                if (Owner.Realm == 0 || SpellHandler.Caster.Realm == 0)
                    Owner.LastAttackTickPvE = GameLoop.GameLoopTime;
                else
                    Owner.LastAttackTickPvP = GameLoop.GameLoopTime;
            }

            if (LastTick == 0)
            {
                LastTick = GameLoop.GameLoopTime;
                NextTick = LastTick + PulseFreq;
            }
            else
            {
                LastTick += PulseFreq;
                NextTick = LastTick + PulseFreq;
            }
            
            if(SpellHandler.Caster is GameSummonedPet)
                Owner.StartInterruptTimer(SpellHandler.Caster.SpellInterruptDuration, AttackData.EAttackType.Spell, SpellHandler.Caster);
                
        }
    }
}