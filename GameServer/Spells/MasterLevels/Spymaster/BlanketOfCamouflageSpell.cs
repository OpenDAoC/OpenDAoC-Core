using System;
using Core.Events;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Spells
{
    #region Spymaster-10
    [SpellHandler("BlanketOfCamouflage")]
    public class BlanketOfCamouflage : MasterLevelSpellHandling
    {
        public BlanketOfCamouflage(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        private GameSpellEffect m_effect;

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (selectedTarget == Caster) return false;
            return base.CheckBeginCast(selectedTarget);
        }
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            m_effect = effect;
            if (effect.Owner is GamePlayer)
            {
                GamePlayer playerTarget = effect.Owner as GamePlayer;
                playerTarget.Stealth(true);
                if (effect.Owner != Caster)
                {
                    //effect.Owner.BuffBonusCategory1[(int)eProperty.Skill_Stealth] += 80;
                    GameEventMgr.AddHandler(playerTarget, GamePlayerEvent.Moving, new CoreEventHandler(PlayerAction));
                    GameEventMgr.AddHandler(playerTarget, GamePlayerEvent.AttackFinished, new CoreEventHandler(PlayerAction));
                    GameEventMgr.AddHandler(playerTarget, GamePlayerEvent.CastStarting, new CoreEventHandler(PlayerAction));
                    GameEventMgr.AddHandler(playerTarget, GamePlayerEvent.Dying, new CoreEventHandler(PlayerAction));
                }
            }
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner != Caster && effect.Owner is GamePlayer)
            {
                //effect.Owner.BuffBonusCategory1[(int)eProperty.Skill_Stealth] -= 80;
                GamePlayer playerTarget = effect.Owner as GamePlayer;
                GameEventMgr.RemoveHandler(playerTarget, GamePlayerEvent.AttackFinished, new CoreEventHandler(PlayerAction));
                GameEventMgr.RemoveHandler(playerTarget, GamePlayerEvent.CastStarting, new CoreEventHandler(PlayerAction));
                GameEventMgr.RemoveHandler(playerTarget, GamePlayerEvent.Moving, new CoreEventHandler(PlayerAction));
                GameEventMgr.RemoveHandler(playerTarget, GamePlayerEvent.Dying, new CoreEventHandler(PlayerAction));
                playerTarget.Stealth(false);
            }
            return base.OnEffectExpires(effect, noMessages);
        }
        private void PlayerAction(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = (GamePlayer)sender;
            if (player == null) return;
            if (args is AttackFinishedEventArgs)
            {
                MessageToLiving((GameLiving)player, "You are attacking. Your camouflage fades!", EChatType.CT_SpellResisted);
                OnEffectExpires(m_effect, true);
                return;
            }
            if (args is DyingEventArgs)
            {
                OnEffectExpires(m_effect, false);
                return;
            }
            if (args is CastingEventArgs)
            {
                if ((args as CastingEventArgs).SpellHandler.Caster != Caster)
                    return;
                MessageToLiving((GameLiving)player, "You are casting a spell. Your camouflage fades!", EChatType.CT_SpellResisted);
                OnEffectExpires(m_effect, true);
                return;
            }
            if (e == GamePlayerEvent.Moving)
            {
                MessageToLiving((GameLiving)player, "You are moving. Your camouflage fades!", EChatType.CT_SpellResisted);
                OnEffectExpires(m_effect, true);
                return;
            }
        }
    }
    #endregion
}