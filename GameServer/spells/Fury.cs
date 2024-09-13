using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Fury)]
    public class FuryHandler : SpellHandler
    {
        public override void OnEffectStart(GameSpellEffect effect)
        {
            int value = (int)m_spell.Value;

            SendEffectAnimation(effect.Owner, 0, false, 1);
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Body] += value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Cold] += value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Energy] += value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Heat] += value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Matter] += value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Spirit] += value;

            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Out.SendCharStatsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendCharResistsUpdate();
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            int value = (int)m_spell.Value;

            effect.Owner.AbilityBonus[(int)eProperty.Resist_Body] -= value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Cold] -= value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Energy] -= value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Heat] -= value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Matter] -= value;
            effect.Owner.AbilityBonus[(int)eProperty.Resist_Spirit] -= value;

            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Out.SendCharStatsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendCharResistsUpdate();
            }

            return 0;
        }

        public FuryHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
