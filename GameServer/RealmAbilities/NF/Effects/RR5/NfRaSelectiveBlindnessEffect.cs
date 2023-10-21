using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Events;

namespace Core.GS.Effects
{
    public class NfRaSelectiveBlindnessEffect : TimedEffect
    {
        private GameLiving EffectOwner;
        private GameLiving m_EffectSource;

        public NfRaSelectiveBlindnessEffect(GameLiving source)
            : base(RealmAbilities.NfRaSelectiveBlindnessAbility.DURATION)
        {
            	m_EffectSource = source as GamePlayer;       
        }
        
        public GameLiving EffectSource
        {
        	get {
        		return m_EffectSource;
        	}
        }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            EffectOwner = target;
            foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, 7059, 0, false, 1);
            }
          	GameEventMgr.AddHandler(EffectSource, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));        
           	GameEventMgr.AddHandler(EffectSource, GameLivingEvent.CastFinished, new CoreEventHandler(EventHandler));                        
         }
        public override void Stop()
        {
            if (EffectOwner != null)
            {
                GameEventMgr.RemoveHandler(EffectSource, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));
           	 	GameEventMgr.RemoveHandler(EffectSource, GameLivingEvent.CastFinished, new CoreEventHandler(EventHandler));                              
           }

            base.Stop();
        }

        /// <summary>
        /// Event that will make effect stops
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        protected void EventHandler(CoreEvent e, object sender, EventArgs args)
        {       	
 			Cancel(false);
        }


        public override string Name { get { return "Selective Blindness"; } }
        public override ushort Icon { get { return 3058; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("You can't attack an ennemy.");
                return list;
            }
        }
    }
}

