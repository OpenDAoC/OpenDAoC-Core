using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.RealmAbilities;

namespace Core.GS.Effects
{
    public class NfRaShadowShroudEffect : TimedEffect
    {
        private GamePlayer EffectOwner;

        public NfRaShadowShroudEffect()
            : base(RealmAbilities.NfRaShadowShroudAbility.DURATION)
        { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, NfRaShadowShroudAbility.EFFECT, 0, false, 1);
                }

                EffectOwner.AbilityBonus[(int)EProperty.MissHit] += NfRaShadowShroudAbility.MISSHITBONUS;
                GameEventMgr.AddHandler(EffectOwner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
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
                EffectOwner.AbilityBonus[(int)EProperty.MissHit] -= NfRaShadowShroudAbility.MISSHITBONUS;
                GameEventMgr.RemoveHandler(EffectOwner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
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

            NfRaShadowShroudEffect ShadowShroud = (NfRaShadowShroudEffect)player.EffectList.GetOfType<NfRaShadowShroudEffect>();
            if (ShadowShroud != null)
                ShadowShroud.Cancel(false);
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        protected void OnAttack(CoreEvent e, object sender, EventArgs arguments)
        {
            AttackedByEnemyEventArgs args = arguments as AttackedByEnemyEventArgs;
            if (args == null) return;
            if (args.AttackData == null) return;
          
            AttackData ad = args.AttackData;
            GameLiving living = sender as GameLiving;
            if (living == null) return;
            double absorbPercent = NfRaShadowShroudAbility.ABSPERCENT;
            int damageAbsorbed = (int)(0.01 * absorbPercent * (ad.Damage + ad.CriticalDamage));
            if (damageAbsorbed > 0)
            {
                ad.Damage -= damageAbsorbed;
                //TODO correct messages
                MessageToLiving(ad.Target, string.Format("Shadow Shroud Ability absorbs {0} damage!", damageAbsorbed), EChatType.CT_Spell);
                MessageToLiving(ad.Attacker, string.Format("A barrier absorbs {0} damage of your attack!", damageAbsorbed), EChatType.CT_Spell);
            }
        }

        /// <summary>
        /// sends a message to a living
        /// </summary>
        /// <param name="living"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public void MessageToLiving(GameLiving living, string message, EChatType type)
        {
            if (living is GamePlayer && message != null && message.Length > 0)
            {
                living.MessageToSelf(message, type);
            }
        }
        public override string Name { get { return "Shadow Shroud"; } }
        public override ushort Icon { get { return 1842; } }

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Reduce all incoming damage by 10% and increase the Nightshade�s chance to be missed by 10% for 30 seconds");
                return list;
            }
        }
    }

}