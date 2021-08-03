using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// Holds the possible activeQuiverSlot values
    /// </summary>
    public enum eActiveQuiverSlot : byte
    {
        /// <summary>
        /// No quiver slot active
        /// </summary>
        None = 0x00,
        /// <summary>
        /// First quiver slot
        /// </summary>
        First = 0x10,
        /// <summary>
        /// Second quiver slot
        /// </summary>
        Second = 0x20,
        /// <summary>
        /// Third quiver slot
        /// </summary>
        Third = 0x40,
        /// <summary>
        /// Fourth quiver slot
        /// </summary>
        Fourth = 0x80,
    }
}
