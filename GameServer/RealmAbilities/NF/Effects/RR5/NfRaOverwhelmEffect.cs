using System;
using System.Collections.Generic;
using DOL.Events;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Effects
{
    /// <summary>
    /// Overwhelm effect Infi RA
    /// </summary>
    /// /// <author>Stexx</author>
    public class NfRaOverwhelmEffect : TimedEffect
    {
        private GamePlayer EffectOwner;

        public NfRaOverwhelmEffect()
            : base(RealmAbilities.NfRaOverwhelmHandler.DURATION)
        { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, NfRaOverwhelmHandler.EFFECT , 0, false, 1);
                }
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Dying, new CoreEventHandler(PlayerLeftWorld));
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerLeftWorld));
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.RegionChanged, new CoreEventHandler(PlayerLeftWorld));
            }
        }
        public override void Stop()
        {
            if (EffectOwner != null)
            {
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Dying, new CoreEventHandler(PlayerLeftWorld));
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerLeftWorld));
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.RegionChanged, new CoreEventHandler(PlayerLeftWorld));
            }
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
            GamePlayer player = sender as GamePlayer;

            NfRaOverwhelmEffect overwhelm = (NfRaOverwhelmEffect)player.EffectList.GetOfType<NfRaOverwhelmEffect>();
            if (overwhelm != null)
                overwhelm.Cancel(false);
        }

        public override string Name { get { return "Overwhelm"; } }
        public override ushort Icon { get { return 1841; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("a 15% increased chance to bypass their target�s block, parry, and evade defenses for 30 seconds.");
                return list;
            }
        }
    }

}