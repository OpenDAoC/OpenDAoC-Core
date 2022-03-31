using System;
using DOL.Events;

namespace DOL.GS.Effects
{
    public class BunkerOfFaithECSEffect : ECSGameAbilityEffect
    {
        public BunkerOfFaithECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.BunkerOfFaith;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4242; } }
        public override string Name { get { return "Bunker of Faith"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;
            GameEventMgr.AddHandler(OwnerPlayer, GamePlayerEvent.Quit, new DOLEventHandler(PlayerLeftWorld));
            OwnerPlayer.AbilityBonus[(int)eProperty.ArmorAbsorption] += (int)Effectiveness;
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;
            GameEventMgr.RemoveHandler(OwnerPlayer, GamePlayerEvent.Quit, new DOLEventHandler(PlayerLeftWorld));
            OwnerPlayer.AbilityBonus[(int)eProperty.ArmorAbsorption] -= (int)Effectiveness;
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        private static void PlayerLeftWorld(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            if (player != null && player.effectListComponent != null)
            {
                EffectService.RequestImmediateCancelEffect(EffectListService.GetEffectOnTarget(player, eEffect.BunkerOfFaith));
            }
        }
    }
}
