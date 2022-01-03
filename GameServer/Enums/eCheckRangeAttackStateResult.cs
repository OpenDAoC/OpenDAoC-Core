using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// The possible results for prechecks for range attacks
    /// </summary>
    public enum eCheckRangeAttackStateResult
    {
        /// <summary>
        /// Hold the shot/throw
        /// </summary>
        Hold,
        /// <summary>
        /// Fire the shot/throw
        /// </summary>
        Fire,
        /// <summary>
        /// Stop the attack
        /// </summary>
        Stop
    }
}
