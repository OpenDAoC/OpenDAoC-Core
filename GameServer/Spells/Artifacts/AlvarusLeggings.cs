using System;
using Core.Events;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS.Spells
{
    [SpellHandler("AlvarusMorph")]
    public class AlvarusMorph : MorphSpell
    {
        GameSpellEffect m_effect = null;
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GamePlayer targetPlayer = target as GamePlayer;

            if (targetPlayer == null)
                return;

            if (!targetPlayer.IsUnderwater)
            {
                MessageToCaster("You must be under water to use this ability.", EChatType.CT_SpellResisted);
                return;
            }
            foreach (GameSpellEffect Effect in targetPlayer.EffectList.GetAllOfType<GameSpellEffect>())
            {
                if (
                    Effect.SpellHandler.Spell.SpellType.Equals("ShadesOfMist") || 
                    Effect.SpellHandler.Spell.SpellType.Equals("TraitorsDaggerProc") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("DreamMorph") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("DreamGroupMorph") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("MaddeningScalars") ||
                    Effect.SpellHandler.Spell.SpellType.Equals("AtlantisTabletMorph"))
                {
                    targetPlayer.Out.SendMessage("You already have an active morph!", EChatType.CT_SpellResisted, EChatLoc.CL_ChatWindow);
                    return;
                }
            }
            base.ApplyEffectOnTarget(target);
        }
        public override void OnEffectStart(GameSpellEffect effect)
        {
            m_effect = effect;
            base.OnEffectStart(effect);
            GamePlayer player = effect.Owner as GamePlayer;
            if (player == null) return;
            GameEventMgr.AddHandler((GamePlayer)effect.Owner, GamePlayerEvent.SwimmingStatus, new CoreEventHandler(SwimmingStatusChange));

        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            if (player == null) return base.OnEffectExpires(effect, noMessages);
            GameEventMgr.RemoveHandler((GamePlayer)effect.Owner, GamePlayerEvent.SwimmingStatus, new CoreEventHandler(SwimmingStatusChange));  
            return base.OnEffectExpires(effect, noMessages);
        }        
        private void SwimmingStatusChange(CoreEvent e, object sender, EventArgs args)
        {
            OnEffectExpires(m_effect, true);
        }        
        public AlvarusMorph(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

    }
}