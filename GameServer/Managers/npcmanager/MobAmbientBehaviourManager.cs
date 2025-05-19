using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;

namespace DOL.GS
{
    /// <summary>
    /// MobAmbientBehaviourManager handles Mob Ambient Behaviour Lazy Loading
    /// </summary>
    public sealed class MobAmbientBehaviourManager
    {
        /// <summary>
        /// Mob X Ambient Behaviour Cache indexed by Mob Name
        /// </summary>
        private Dictionary<string, DbMobXAmbientBehavior[]> AmbientBehaviour { get; }

        /// <summary>
        /// Retrieve MobXambiemtBehaviour Objects from Mob Name
        /// </summary>
        public DbMobXAmbientBehavior[] this[string index]
        {
            get
            {
                if (string.IsNullOrEmpty(index))
                    return [];

                return AmbientBehaviour.TryGetValue(index.ToLower(), out DbMobXAmbientBehavior[] value) ? value : [];
            }
        }

        /// <summary>
        /// Create a new Instance of <see cref="MobAmbientBehaviourManager"/>
        /// </summary>
        public MobAmbientBehaviourManager(IObjectDatabase database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            AmbientBehaviour = database.SelectAllObjects<DbMobXAmbientBehavior>()
                .GroupBy(x => x.Source)
                .ToDictionary(key => key.Key.ToLower(), value => value.ToArray());
        }
    }
}
