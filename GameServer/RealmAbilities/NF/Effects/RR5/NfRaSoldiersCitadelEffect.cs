using System;
using System.Collections.Generic;
using DOL.Events;

namespace DOL.GS.Effects
{
    public class NfRaSoldiersCitadelEffect : TimedEffect
    {
		private GamePlayer EffectOwner;

        public NfRaSoldiersCitadelEffect()
            : base(RealmAbilities.NfRaSoldiersCitadelAbility.DURATION)
        { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, 7093, 0, false, 1);
                }
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.ParryChance] += 50;
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.BlockChance] += 50;
				
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
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.ParryChance] -= 50;
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.BlockChance] -= 50;
                if(EffectOwner.IsAlive) new SoldiersCitadelSecondaryEffect().Start(EffectOwner);
				
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

			NfRaSoldiersCitadelEffect SoldiersCitadel = player.EffectList.GetOfType<NfRaSoldiersCitadelEffect>();
			if (SoldiersCitadel != null)
				SoldiersCitadel.Cancel(false);
		}

        public override string Name { get { return "Soldier's Citadel"; } }
        public override ushort Icon { get { return 3091; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Grants +50% block/parry for 30s.");
                return list;
            }
        }
    }
    public class SoldiersCitadelSecondaryEffect : TimedEffect
    {
        private GamePlayer EffectOwner;

        public SoldiersCitadelSecondaryEffect()
            : base(RealmAbilities.NfRaSoldiersCitadelAbility.SECOND_DURATION)
        { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.ParryChance] -= 10;
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.BlockChance] -= 10;
				
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
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.ParryChance] += 10;
                EffectOwner.BaseBuffBonusCategory[(int)EProperty.BlockChance] += 10;
				
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
        private static void PlayerLeftWorld(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

			SoldiersCitadelSecondaryEffect SoldiersCitadel = player.EffectList.GetOfType<SoldiersCitadelSecondaryEffect>();
            if (SoldiersCitadel != null)
                SoldiersCitadel.Cancel(false);
       }

        public override string Name { get { return "Soldier's Citadel"; } }
        public override ushort Icon { get { return 3091; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Penality -10% block/parry for 15s");
                return list;
            }
        }
    }
}
