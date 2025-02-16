using System.Reflection;
using DOL.GS;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain that can be controlled
    /// </summary>
    public class BdDebufferBrain : BdPetBrain
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs new controlled npc brain
        /// </summary>
        /// <param name="owner"></param>
        public BdDebufferBrain(GameLiving owner) : base(owner) { }

        #region AI

        /// <summary>
        /// Checks the Abilities
        /// </summary>
        public override void CheckAbilities() { }

        #endregion
    }
}
