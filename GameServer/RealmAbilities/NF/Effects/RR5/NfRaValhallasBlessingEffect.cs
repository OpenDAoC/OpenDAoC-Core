using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Events;

namespace Core.GS.Effects
{
    public class NfRaValhallasBlessingEffect : TimedEffect
    {
        private GamePlayer EffectOwner;

        public NfRaValhallasBlessingEffect()
            : base(RealmAbilities.NfRaValhallasBlessingAbility.DURATION)
        { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    p.Out.SendSpellEffectAnimation(target, target, 7087, 0, false, 1);
                }
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
            }
        }
        public override void Stop()
        {
            if (EffectOwner != null)
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
            base.Stop();
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        protected void PlayerLeftWorld(CoreEvent e, object sender, EventArgs args)
        {
 			Cancel(false);
        }

        public override string Name { get { return "Valhalla's Blessing"; } }
        public override ushort Icon { get { return 3086; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Spells/Styles have has a chance of not costing power or endurance.");
                return list;
            }
        }
    }
}
