using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.Spells
{
    #region CCResist
    [SpellHandler("CCResist")]
    public class CcResistSpell : MasterLevelSpellHandling
    {
        public override void OnEffectStart(GameSpellEffect effect)
        {
        	base.OnEffectStart(effect);
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.MesmerizeDurationReduction] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.StunDurationReduction] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.SpeedDecreaseDurationReduction] += (int)m_spell.Value;
             
            if (effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();       
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.MesmerizeDurationReduction] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.StunDurationReduction] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[(int)EProperty.SpeedDecreaseDurationReduction] -= (int)m_spell.Value;
            
            if (effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();  
            }
            return base.OnEffectExpires(effect,noMessages);
        }

        // constructor
        public CcResistSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}
