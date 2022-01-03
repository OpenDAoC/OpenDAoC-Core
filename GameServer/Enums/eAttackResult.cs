using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// The result of an attack
    /// </summary>
    public enum eAttackResult : int
    {
        /// <summary>
        /// No specific attack
        /// </summary>
        Any = 0,
        /// <summary>
        /// The attack was a hit
        /// </summary>
        HitUnstyled = 1,
        /// <summary>
        /// The attack was a hit
        /// </summary>
        HitStyle = 2,
        /// <summary>
        /// Attack was denied by server rules
        /// </summary>
        NotAllowed_ServerRules = 3,
        /// <summary>
        /// No target for the attack
        /// </summary>
        NoTarget = 5,
        /// <summary>
        /// Target is already dead
        /// </summary>
        TargetDead = 6,
        /// <summary>
        /// Target is out of range
        /// </summary>
        OutOfRange = 7,
        /// <summary>
        /// Attack missed
        /// </summary>
        Missed = 8,
        /// <summary>
        /// The attack was evaded
        /// </summary>
        Evaded = 9,
        /// <summary>
        /// The attack was blocked
        /// </summary>
        Blocked = 10,
        /// <summary>
        /// The attack was parried
        /// </summary>
        Parried = 11,
        /// <summary>
        /// The target is invalid
        /// </summary>
        NoValidTarget = 12,
        /// <summary>
        /// The target is not visible
        /// </summary>
        TargetNotVisible = 14,
        /// <summary>
        /// The attack was fumbled
        /// </summary>
        Fumbled = 15,
        /// <summary>
        /// The attack was Bodyguarded
        /// </summary>
        Bodyguarded = 16,
        /// <summary>
        /// The attack was Phaseshiftet
        /// </summary>
        Phaseshift = 17,
        /// <summary>
        /// The attack was Grappled
        /// </summary>
        Grappled = 18
    }
}
