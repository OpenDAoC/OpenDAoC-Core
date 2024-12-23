using DOL.GS.ServerProperties;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class NpcStyleComponent : StyleComponent
    {
        private GameNPC _npcOwner;

        public NpcStyleComponent(GameNPC npcOwner) : base(npcOwner)
        {
            _npcOwner = npcOwner;
        }

        public override Style GetStyleToUse()
        {
            // We return `NextCombatStyle` if it was overwritten.
            // It should be set back to null once used.
            if (NextCombatStyle != null)
                return NextCombatStyle;

            if (_npcOwner.Styles == null || _npcOwner.Styles.Count < 1 || _npcOwner.TargetObject == null)
                return null;

            AttackData lastAttackData = _npcOwner.attackComponent.attackAction.LastAttackData;

            // Chain and defensive styles are excluded from the chance roll because they would almost never happen otherwise. 
            // For example, an NPC blocks 10% of the time, so the default 20% style chance effectively means the defensive 
            // style would only actually occur during 2% of of a mob's attacks. In comparison, a style chain would only happen 
            // 0.4% of the time.
            if (_npcOwner.StylesChain != null && _npcOwner.StylesChain.Count > 0)
            {
                foreach (Style style in _npcOwner.StylesChain)
                {
                    if (StyleProcessor.CanUseStyle(lastAttackData, _npcOwner, style, _npcOwner.ActiveWeapon))
                        return style;
                }
            }

            if (_npcOwner.StylesDefensive != null && _npcOwner.StylesDefensive.Count > 0)
            {
                foreach (Style style in _npcOwner.StylesDefensive)
                {
                    if (StyleProcessor.CanUseStyle(lastAttackData, _npcOwner, style, _npcOwner.ActiveWeapon) && _npcOwner.CheckStyleStun(style)) // Make sure we don't spam stun styles like Brutalize
                        return style;
                }
            }

            if (Util.Chance(Properties.GAMENPC_CHANCES_TO_STYLE))
            {
                // All of the remaining lists are randomly picked from,
                // as this creates more variety with each combat result.
                // For example, a mob with both Pincer and Ice Storm
                // styles could potentially use one or the other with
                // each attack roll that succeeds.

                // First, check positional styles (in order of back, side, front)
                // in case the defender is facing another direction
                if (_npcOwner.StylesBack != null && _npcOwner.StylesBack.Count > 0)
                {
                    Style style = _npcOwner.StylesBack[Util.Random(0, _npcOwner.StylesBack.Count - 1)];

                    if (StyleProcessor.CanUseStyle(lastAttackData, _npcOwner, style, _npcOwner.ActiveWeapon))
                        return style;
                }

                if (_npcOwner.StylesSide != null && _npcOwner.StylesSide.Count > 0)
                {
                    Style style = _npcOwner.StylesSide[Util.Random(0, _npcOwner.StylesSide.Count - 1)];

                    if (StyleProcessor.CanUseStyle(lastAttackData, _npcOwner, style, _npcOwner.ActiveWeapon))
                        return style;
                }

                if (_npcOwner.StylesFront != null && _npcOwner.StylesFront.Count > 0)
                {
                    Style style = _npcOwner.StylesFront[Util.Random(0, _npcOwner.StylesFront.Count - 1)];

                    if (StyleProcessor.CanUseStyle(lastAttackData, _npcOwner, style, _npcOwner.ActiveWeapon))
                        return style;
                }

                // Pick a random anytime style
                if (_npcOwner.StylesAnytime != null && _npcOwner.StylesAnytime.Count > 0)
                    return _npcOwner.StylesAnytime[Util.Random(0, _npcOwner.StylesAnytime.Count - 1)];
            }

            return null;
        }
    }
}
