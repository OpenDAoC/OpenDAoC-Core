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
            EffectType = eEffect.MovementSpeedBuff;
            EffectService.RequestStartEffect(this);
        }

        DOLEventHandler m_attackFinished = new DOLEventHandler(AttackFinished);

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
        }

        public override ushort Icon
        {
            get { return 3020; }
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
            
            GameEventMgr.AddHandler(OwnerPlayer, GameLivingEvent.CastFinished, m_attackFinished);
            OwnerPlayer.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, this,
                PropertyCalc.MaxSpeedCalculator.SPEED4);
            OwnerPlayer.Out.SendUpdateMaxSpeed();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);
            OwnerPlayer.Out.SendUpdateMaxSpeed();
            GameEventMgr.RemoveHandler(OwnerPlayer, GameLivingEvent.CastFinished, m_attackFinished);
        }
    }
}