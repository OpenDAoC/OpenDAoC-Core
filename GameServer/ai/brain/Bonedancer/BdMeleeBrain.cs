using System.Reflection;
using DOL.GS;
using DOL.GS.RealmAbilities;
using log4net;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain that can be controlled
    /// </summary>
    public class BdMeleeBrain : BdPetBrain
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs new controlled npc brain
        /// </summary>
        /// <param name="owner"></param>
        public BdMeleeBrain(GameLiving owner) : base(owner) { }

        #region AI

        /// <summary>
        /// Checks the Abilities
        /// </summary>
        public override void CheckAbilities()
        {
            //load up abilities
            if (Body.Abilities != null && Body.Abilities.Count > 0)
            {
                foreach (Ability ab in Body.Abilities.Values)
                {
                    switch (ab.KeyName)
                    {
                        case Abilities.ChargeAbility:
                            if (Body.TargetObject != null && !Body.IsWithinRadius(Body.TargetObject, 500 ))
                            {
                                ChargeAbility charge = Body.GetAbility<ChargeAbility>();
                                if (charge != null && Body.GetSkillDisabledDuration(charge) <= 0)
                                {
                                    charge.Execute(Body);
                                }
                            }
                            break;
                    }
                }
            }
        }

        public override bool CheckSpells(eCheckSpellType type) { return false; }

        #endregion
    }
}
