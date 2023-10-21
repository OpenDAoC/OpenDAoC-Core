using System;
using System.Collections.Generic;
using Core.Events;

namespace Core.GS.Effects
{
    public class NfRaBoilingCauldronEffect : TimedEffect
    {
        // Parameters
        private const int cauldronModel = 2947; 		// Model to use for cauldron
        private const int cauldronLevel = 50;			// Cauldron level		
        private const string cauldronName = "Cauldron";	// Name of cauldron
        private const int spellDamage = 650;			// Damage inflicted
        private const ushort spellRadius = 350;			// Spell radius

        // Objects
        private GamePlayer EffectOwner;				// Effect owner
        private GameStaticItem Cauldron;					// The cauldron

        public NfRaBoilingCauldronEffect()
            : base(RealmAbilities.NfRaBoilingCauldronAbility.DURATION)
        { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, 7086, 0, false, 1);
                }
                SummonCauldron();
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
            }
        }
        public override void Stop()
        {
            if (Cauldron != null) { Cauldron.Delete(); Cauldron = null; }
            if (EffectOwner != null)
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));

            base.Stop();
        }

        // Summon the cauldron
        private void SummonCauldron()
        {
            Cauldron = new GameStaticItem();
            Cauldron.CurrentRegion = EffectOwner.CurrentRegion;
            Cauldron.Heading = (ushort)((EffectOwner.Heading + 2048) % 4096);
            Cauldron.Level = cauldronLevel;
            Cauldron.Realm = EffectOwner.Realm;
            Cauldron.Name = cauldronName;
            Cauldron.Model = cauldronModel;
            Cauldron.X = EffectOwner.X;
            Cauldron.Y = EffectOwner.Y;
            Cauldron.Z = EffectOwner.Z;
            Cauldron.AddToWorld();

            new EcsGameTimer(EffectOwner, new EcsGameTimer.EcsTimerCallback(CauldronCallBack), RealmAbilities.NfRaBoilingCauldronAbility.DURATION - 1000);
        }

        private int CauldronCallBack(EcsGameTimer timer)
        {
            if (Cauldron != null && EffectOwner != null)
            {
                foreach (GamePlayer target in Cauldron.GetPlayersInRadius(spellRadius))
                {
                    if (GameServer.ServerRules.IsAllowedToAttack(EffectOwner, target, true))
                        target.TakeDamage(EffectOwner, EDamageType.Heat, spellDamage, 0);
                }
            }
            timer.Stop();
            timer = null;
            return 0;
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

        public override string Name { get { return "Boiling Cauldron"; } }
        public override ushort Icon { get { return 3085; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Cauldron that boil in place for 5s before spilling and doing damage to all those nearby.");
                return list;
            }
        }
    }
}

