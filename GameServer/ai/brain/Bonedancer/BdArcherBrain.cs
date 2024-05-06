using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain that can be controlled
    /// </summary>
    public class BDArcherBrain : BDPetBrain
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs new controlled npc brain
        /// </summary>
        /// <param name="owner"></param>
        public BDArcherBrain(GameLiving owner) : base(owner) { }

        #region AI

        /// <summary>
        /// No Abilities or spells
        /// </summary>
        public override void CheckAbilities() { }
        public override bool CheckSpells(eCheckSpellType type) { return false; }

        public override void Attack(GameObject target)
        {
            if (m_orderAttackTarget != target)
                Body.SwitchWeapon(eActiveWeaponSlot.Distance);

            base.Attack(target);
        }

        #endregion
    }
}
