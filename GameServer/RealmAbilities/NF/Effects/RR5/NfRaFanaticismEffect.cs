using System;
using System.Collections.Generic;
using DOL.Events;

namespace DOL.GS.Effects
{
    /// <summary>
    /// Effect handler for Fanaticism
    /// </summary>
    public class NfRaFanaticismEffect : TimedEffect
    {
 		private GamePlayer EffectOwner;
 		
        public NfRaFanaticismEffect()
            : base(RealmAbilities.NfRaFanaticismHandler.DURATION)
        { }    

         public override void Start(GameLiving target)
        {
        	base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    p.Out.SendSpellEffectAnimation(EffectOwner, p, 7088, 0, false, 1);
                }
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
            	EffectOwner.BaseBuffBonusCategory[(int)EProperty.MagicAbsorption] += RealmAbilities.NfRaFanaticismHandler.VALUE;
            }
        }

        public override void Stop()
        {
            if (EffectOwner != null)
            {
            	EffectOwner.BaseBuffBonusCategory[(int)EProperty.MagicAbsorption] -= RealmAbilities.NfRaFanaticismHandler.VALUE;
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
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
  			Cancel(false);
        }

        public override string Name { get { return "Fanaticism"; } }
        public override ushort Icon { get { return 7088; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Grants a reduction in all spell damage taken for 45 seconds.");
                return list;
            }
        }
    }
}