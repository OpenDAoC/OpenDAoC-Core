using System.Linq;
using Core.GS.Calculators;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Effects
{
    public class OfRaSpeedOfSoundEcsEffect : EcsGameAbilityEffect
    {
        public OfRaSpeedOfSoundEcsEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.SpeedOfSound;
            EffectService.RequestStartEffect(this);
        }

        // removed handler as OF SOS doesn't break on attack - yay minstrels..
        // DOLEventHandler m_attackFinished = new DOLEventHandler(AttackFinished);

        /*
        /// <summary>
        /// Called when the effectowner attacked an enemy
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        private static void AttackFinished(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = (GamePlayer) sender;
            if (e == GameLivingEvent.CastFinished)
            {
                CastingEventArgs cfea = args as CastingEventArgs;
                
                if (cfea.SpellHandler.Caster != player)
                    return;

                //cancel if the effectowner casts a non-positive spell
                if (!cfea.SpellHandler.HasPositiveEffect)
                {
                    EffectService.RequestCancelEffect(player.effectListComponent.GetAllEffects().FirstOrDefault(x => x.Name.Equals("Speed Of Sound")));
                }
            }
        }*/

        public override ushort Icon
        {
            get { return 4249; }
        }

        public override string Name
        {
            get { return "Speed Of Sound"; }
        }

        public override bool HasPositiveEffect
        {
            get { return true; }
        }

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;
            
            // removed handler as OF SOS doesn't break on attack - yay minstrels..
            // GameEventMgr.AddHandler(OwnerPlayer, GameLivingEvent.CastFinished, m_attackFinished);
            foreach (var speedBuff in OwnerPlayer.effectListComponent.GetSpellEffects(EEffect.MovementSpeedBuff))
            {
                if(speedBuff.GetType() != typeof(OfRaSpeedOfSoundEcsEffect))
                    EffectService.RequestDisableEffect(speedBuff);
            }

            foreach (var snare in OwnerPlayer.effectListComponent.GetSpellEffects(EEffect.Snare))
            {
                EffectService.RequestDisableEffect(snare);
            }
            
            foreach (var root in OwnerPlayer.effectListComponent.GetSpellEffects(EEffect.MovementSpeedDebuff))
            {
                EffectService.RequestDisableEffect(root);
            }
            
            if(OwnerPlayer.effectListComponent.ContainsEffectForEffectType(EEffect.Ichor))
                EffectService.RequestDisableEffect(OwnerPlayer.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == EEffect.Ichor));
            
            OwnerPlayer.BuffBonusMultCategory1.Set((int) EProperty.MaxSpeed, this,
                MaxMovementSpeedCalculator.SPEED4);
            OwnerPlayer.Out.SendUpdateMaxSpeed();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, this);
            if (OwnerPlayer.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedBuff))
            {
                var speedBuff = OwnerPlayer.effectListComponent.GetBestDisabledSpellEffect(EEffect.MovementSpeedBuff);

                if (speedBuff != null)
                {
                    speedBuff.IsBuffActive = false;
                    EffectService.RequestEnableEffect(speedBuff);                   
                }
            }
            
            foreach (var snare in OwnerPlayer.effectListComponent.GetSpellEffects(EEffect.Snare))
            {
                EffectService.RequestEnableEffect(snare);
            }
            
            foreach (var root in OwnerPlayer.effectListComponent.GetSpellEffects(EEffect.MovementSpeedDebuff))
            {
                EffectService.RequestEnableEffect(root);
            }
            
            if(OwnerPlayer.effectListComponent.ContainsEffectForEffectType(EEffect.Ichor))
                EffectService.RequestEnableEffect(OwnerPlayer.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == EEffect.Ichor));
            
            OwnerPlayer.Out.SendUpdateMaxSpeed();
            
            // removed handler as OF SOS doesn't break on attack - yay minstrels..
            // GameEventMgr.RemoveHandler(OwnerPlayer, GameLivingEvent.CastFinished, m_attackFinished);
        }
    }
}