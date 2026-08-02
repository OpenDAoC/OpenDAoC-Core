using System;
using System.Collections.Concurrent;
using DOL.Database;

namespace DOL.GS
{
    /// <summary>
    /// Spell lookup and synthetic spell caching for mob scripts.
    /// </summary>
    public static class ScriptSpells
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ConcurrentDictionary<string, Spell> _synthetic = new();

        public static Spell FromDatabase(int spellId)
        {
            Spell spell = SkillBase.GetSpellByID(spellId);

            if (spell == null && log.IsWarnEnabled)
                log.Warn($"ScriptSpells: no DbSpell with SpellID {spellId}");

            return spell;
        }

        public static Spell GetOrCreate(string key, int level, Action<DbSpell> configure)
        {
            return _synthetic.GetOrAdd(key, _ =>
            {
                DbSpell db = new() { AllowAdd = false };
                configure(db);
                return new Spell(db, level);
            });
        }
    }
}
