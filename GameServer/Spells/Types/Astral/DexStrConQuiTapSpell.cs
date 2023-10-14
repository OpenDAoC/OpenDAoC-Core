using System.Collections.Generic;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Dex/Qui/Str/Con stat specline debuff and transfers them to the caster.
    /// </summary>
    [SpellHandler("DexStrConQuiTap")]
    public class DexStrConQuiTapSpell : SpellHandler
    {
        private IList<EProperty> m_stats;
        public IList<EProperty> Stats
        {
            get { return m_stats; }
            set { m_stats = value; }
        }

        public DexStrConQuiTapSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            Stats = new List<EProperty>();
            Stats.Add(EProperty.Dexterity);
            Stats.Add(EProperty.Strength);
            Stats.Add(EProperty.Constitution);
            Stats.Add(EProperty.Quickness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            foreach (EProperty property in Stats)
            {
                m_caster.BaseBuffBonusCategory[(int)property] += (int)m_spell.Value;
                Target.DebuffCategory[(int)property] -= (int)m_spell.Value;
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            foreach (EProperty property in Stats)
            {
                Target.DebuffCategory[(int)property] += (int)m_spell.Value;
                m_caster.BaseBuffBonusCategory[(int)property] -= (int)m_spell.Value;
            }
            return base.OnEffectExpires(effect, noMessages);
        }
    }
}
