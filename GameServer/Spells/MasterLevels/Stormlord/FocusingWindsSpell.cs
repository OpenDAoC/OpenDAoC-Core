using System;
using Core.Events;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Spells
{
    //no shared timer

    [SpellHandler("FocusingWinds")]
    public class FocusingWindsSpell : SpellHandler
    {
        private GameSpellEffect m_effect;

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            m_effect = effect;
            if (effect.Owner is GameStorm)
            {
                GameStorm targetStorm = effect.Owner as GameStorm;
                targetStorm.Movable = false;
                MessageToCaster("Now the vortex of this storm is locked!", EChatType.CT_YouWereHit);
                GameEventMgr.AddHandler(m_caster, GameLivingEvent.Moving, new CoreEventHandler(LivingMoves));
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner is GameStorm)
            {
                GameStorm targetStorm = effect.Owner as GameStorm;
                targetStorm.Movable = true;
                GameEventMgr.RemoveHandler(m_caster, GameLivingEvent.Moving, new CoreEventHandler(LivingMoves));
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        public void LivingMoves(CoreEvent e, object sender, EventArgs args)
        {
            GameLiving player = sender as GameLiving;
            if (player == null) return;
            if (e == GameLivingEvent.Moving)
            {
                MessageToCaster("You are moving. Your concentration fades", EChatType.CT_SpellExpires);
                OnEffectExpires(m_effect, true);
                return;
            }
        }

        public FocusingWindsSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}