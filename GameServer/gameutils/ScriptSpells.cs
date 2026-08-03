using System;
using System.Collections.Concurrent;
using DOL.Database;
using DOL.Logging;

namespace DOL.GS
{
    public static class ScriptSpells
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ConcurrentDictionary<string, Spell> _cache = new();

        public static Spell FromDatabase(int spellId)
        {
            Spell spell = SkillBase.GetSpellByID(spellId);

            if (spell == null && log.IsWarnEnabled)
                log.Warn($"No {nameof(DbSpell)} with {nameof(DbSpell.SpellID)} {spellId}");

            return spell;
        }

        public static Spell GetOrCreate(string key, int level, Action<DbSpell> configure)
        {
            return GetOrCreate(key, level, static (db, cfg) => cfg(db), configure);
        }

        public static Spell GetOrCreate<TState>(string key, int level, Action<DbSpell, TState> configure, TState state)
        {
            return _cache.GetOrAdd(key, static (key, s) =>
            {
                DbSpell db = new() { AllowAdd = false };
                s.configure(db, s.state);
                return new(db, s.level);
            }, (level, configure, state));
        }
    }
}
