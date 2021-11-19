using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class DamageOverTimeECSGameEffect : ECSGameSpellEffect
    {
        public DamageOverTimeECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) 
        {
            NextTick = GameLoop.GameLoopTime;
        }

        public override void OnStartEffect()
        {
            // Remove stealth on first application since the code that normally handles removing stealth on
            // attack ignores DoT damage, since only the first tick of a DoT should remove stealth.            
            if (OwnerPlayer != null)
                OwnerPlayer.Stealth(false);

            if (!IsBuffActive)
                OnEffectPulse();
        }

        public override void OnStopEffect()
        {
            //if (EffectType == eEffect.Bleed)
                //Owner.TempProperties.removeProperty(StyleBleeding.BLEED_VALUE_PROPERTY);
        }

        public override void OnEffectPulse()
        {
            if (Owner.IsAlive == false)
            {
                EffectService.RequestCancelEffect(this);
            }

            if (Owner.IsAlive)
            {
                if (SpellHandler is DoTSpellHandler handler)
                {
                    // An acidic cloud surrounds you!
                    handler.MessageToLiving(Owner, SpellHandler.Spell.Message1, eChatType.CT_Spell);
                    // {0} is surrounded by an acidic cloud!
                    Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, false)), eChatType.CT_YouHit, Owner);

                    handler.OnDirectEffect(Owner, Effectiveness, true);
                }
                else if (SpellHandler is StyleBleeding bleedHandler)
                {
                    if (StartTick + PulseFreq > GameLoop.GameLoopTime && Owner.TempProperties.getProperty<int>(StyleBleeding.BLEED_VALUE_PROPERTY) < bleedHandler.Spell.Damage)
                    {
                        Owner.TempProperties.setProperty(StyleBleeding.BLEED_VALUE_PROPERTY, (int)bleedHandler.Spell.Damage + (int)bleedHandler.Spell.Damage * Util.Random(25) / 100);  // + random max 25%
                    }

                    bleedHandler.MessageToLiving(Owner, bleedHandler.Spell.Message1, eChatType.CT_YouWereHit);
                    Message.SystemToArea(Owner, Util.MakeSentence(bleedHandler.Spell.Message2, Owner.GetName(0, false)), eChatType.CT_YouHit, Owner);

                    int bleedValue = Owner.TempProperties.getProperty<int>(StyleBleeding.BLEED_VALUE_PROPERTY);

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

                    if (--bleedValue <= 0 || !Owner.IsAlive)
                    {
                        EffectService.RequestCancelEffect(this);
                    }
                    else Owner.TempProperties.setProperty(StyleBleeding.BLEED_VALUE_PROPERTY, bleedValue);
                }
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
        }
    }
}
