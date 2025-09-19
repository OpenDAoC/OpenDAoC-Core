using DOL.GS;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain that can be controlled
    /// </summary>
    public class BdArcherBrain : BdPetBrain
    {
        /// <summary>
        /// Constructs new controlled npc brain
        /// </summary>
        /// <param name="owner"></param>
        public BdArcherBrain(GameLiving owner) : base(owner) { }

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
