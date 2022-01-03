using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// The possible states for a ranged attack
    /// </summary>
    public enum eRangedAttackState : byte
    {
        /// <summary>
        /// No ranged attack active
        /// </summary>
        None = 0,
        /// <summary>
        /// Ranged attack in aim-state
        /// </summary>
        Aim,
        /// <summary>
        /// Player wants to fire the shot/throw NOW!
        /// </summary>
        Fire,
        /// <summary>
        /// Ranged attack will fire when ready
        /// </summary>
        AimFire,
        /// <summary>
        /// Ranged attack will fire and reload when ready
        /// </summary>
        AimFireReload,
        /// <summary>
        /// Ranged attack is ready to be fired
        /// </summary>
        ReadyToFire,
    }
}
