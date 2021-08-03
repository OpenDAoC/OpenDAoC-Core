using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// Holds all the ways this living can
    /// be healed
    /// </summary>
    public enum eManaChangeType : byte
    {
        /// <summary>
        /// Unknown mana change
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Mana was changed by regenerate
        /// </summary>
        Regenerate = 1,
        /// <summary>
        /// Mana was changed by spell
        /// </summary>
        Spell = 2,
        /// <summary>
        /// Mana was changed by potion
        /// </summary>
        Potion = 3
    }
}
