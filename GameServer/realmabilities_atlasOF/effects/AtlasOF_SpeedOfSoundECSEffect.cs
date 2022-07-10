using System;
using System.Linq;
using DOL.Events;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Effects
{
    public class SpeedOfSoundECSEffect : ECSGameAbilityEffect
    {
        public SpeedOfSoundECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.SpeedOfSound;
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
            foreach (var speedBuff in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff))
            {
                if(speedBuff.GetType() != typeof(SpeedOfSoundECSEffect))
                    EffectService.RequestDisableEffect(speedBuff);
            }
            
            OwnerPlayer.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, this,
                PropertyCalc.MaxSpeedCalculator.SPEED4);
            OwnerPlayer.Out.SendUpdateMaxSpeed();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);
            if (OwnerPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedBuff))
            {
                var speedBuff = OwnerPlayer.effectListComponent.GetBestDisabledSpellEffect(eEffect.MovementSpeedBuff);

                if (speedBuff != null)
                {
                    speedBuff.IsBuffActive = false;
                    EffectService.RequestEnableEffect(speedBuff);                   
                }
            }
            
            OwnerPlayer.Out.SendUpdateMaxSpeed();
            
            // removed handler as OF SOS doesn't break on attack - yay minstrels..
            // GameEventMgr.RemoveHandler(OwnerPlayer, GameLivingEvent.CastFinished, m_attackFinished);
        }
    }
}