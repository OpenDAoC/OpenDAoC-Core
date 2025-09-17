namespace DOL.GS.Spells
{
    /// <summary>
    /// Paladin heal chant works only in combat
    /// </summary>
    [SpellHandler(eSpellType.CombatHeal)]
    public class CombatHealSpellHandler : HealSpellHandler
    {
        public override string ShortDescription => $"Heals the target for {Spell.Value} hit points every {Spell.Frequency / 1000.0} seconds. Only effective in combat.";

        public CombatHealSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        /// <summary>
        /// Execute heal spell
        /// </summary>
        /// <param name="target"></param>
        public override bool StartSpell(GameLiving target)
        {
            m_startReuseTimer = true;

            foreach (GameLiving member in GetGroupAndPets(Spell))
                ECSGameEffectFactory.Create(new(member, Spell.Frequency, Caster.Effectiveness, this), static (in ECSGameEffectInitParams i) => new CombatHealECSEffect(i));

            GamePlayer player = Caster as GamePlayer;

            if (!Caster.InCombat && (player==null || player.Group==null || !player.Group.IsGroupInCombat()))
                return false; // Do not start healing if not in combat

            return base.StartSpell(target);
        }
    }
}
