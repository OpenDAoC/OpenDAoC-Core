using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("AtlantisTabletMorph")]
    public class AtlantisTabletMorph : SpellHandler
    {
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target is not GamePlayer player) return;
            if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Morph))
            {
                player.Out.SendMessage("You already have an active morph!", DOL.GS.PacketHandler.eChatType.CT_SpellResisted, DOL.GS.PacketHandler.eChatLoc.CL_ChatWindow);
                return;
            }

            new AtlantisTabletMorphECSEffect(new ECSGameEffectInitParams(player, Spell.Duration, 1, this));
        }

        public AtlantisTabletMorph(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}