using System.Collections.Generic;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.PveResurrectionIllness)]
    public class PveResurrectionIllness : AbstractIllnessSpellHandler
    {
        public override string ShortDescription => "The player's effectiveness is greatly reduced due to being recently resurrected.";

        public PveResurrectionIllness(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in i) => new ResurrectionIllnessECSGameEffect(i));
        }

        public override IList<string> DelveInfo =>
            [
                " ",
                ShortDescription,
                " ",
                "- Effectiveness penality: " + Spell.Value + "%"
            ];
    }

    public class AbstractIllnessSpellHandler : SpellHandler
    {
        public AbstractIllnessSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override bool HasPositiveEffect => false;

        public override int CalculateEnduranceCost()
        {
            return 0;
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double modifier = 1.0;
            VeilRecoveryAbility veilRecovery = target.GetAbility<VeilRecoveryAbility>();

            if (veilRecovery != null)
                modifier -= veilRecovery.Amount * 0.01;

            return (int) (Spell.Duration * modifier);
        }
    }
}
