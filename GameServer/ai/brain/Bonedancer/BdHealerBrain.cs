using System.Reflection;
using DOL.GS;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain that can be controlled
    /// </summary>
    public class BdHealerBrain : BdPetBrain
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs new controlled npc brain
        /// </summary>
        /// <param name="owner"></param>
        public BdHealerBrain(GameLiving owner) : base(owner)
        {
            AggroLevel = 0;
            AggroRange = 0;
        }

        #region Control

        /// <summary>
        /// Gets or sets the aggression state of the brain
        /// </summary>
        public override eAggressionState AggressionState
        {
            get { return eAggressionState.Passive; }
            set { }
        }

        /// <summary>
        /// Attack the target on command
        /// </summary>
        /// <param name="target"></param>
        public override void Attack(GameObject target) { }

        #endregion

        #region AI

        /// <summary>
        /// Checks the Abilities
        /// </summary>
        public override void CheckAbilities() { }

        protected override GameLiving FindTargetForDefensiveSpell(Spell spell)
        {
            GameLiving target = null;
            int healThreshold = Properties.BONEDANCER_HEALER_PET_HEAL_THRESHOLD;

            switch (spell.SpellType)
            {
                #region Heals

                case eSpellType.Heal:
                {
                    GamePlayer player = GetPlayerOwner();

                    // Heal player.
                    if (player != null)
                    {
                        if (player.HealthPercent < healThreshold)
                        {
                            target = player;
                            break;
                        }
                    }

                    GameLiving owner = (this as IControlledBrain).Owner;

                    // Heal owner.
                    if (owner.HealthPercent < healThreshold)
                    {
                        target = owner;
                        break;
                    }

                    // Heal self.
                    if (Body.HealthPercent < healThreshold)
                    {
                        target = Body;
                        break;
                    }

                    // Heal other minions.
                    foreach (IControlledBrain icb in ((GameNPC) owner).ControlledNpcList)
                    {
                        if (icb == null)
                            continue;

                        if (icb.Body.HealthPercent < healThreshold)
                        {
                            target = icb.Body;
                            break;
                        }
                    }

                    break;
                }

                #endregion

                #region Buffs

                case eSpellType.HealthRegenBuff:
                {
                    // Buff self.
                    if (!LivingHasEffect(Body, spell))
                    {
                        target = Body;
                        break;
                    }

                    GameLiving owner = (this as IControlledBrain).Owner;

                    // Buff owner.
                    if (owner != null)
                    {
                        GamePlayer player = GetPlayerOwner();

                        // Buff player.
                        if (player != null)
                        {
                            if (!LivingHasEffect(player, spell))
                            {
                                target = player;
                                break;
                            }
                        }

                        if (!LivingHasEffect(owner, spell))
                        {
                            target = owner;
                            break;
                        }

                        //Buff other minions
                        foreach (IControlledBrain icb in ((GameNPC) owner).ControlledNpcList)
                        {
                            if (icb == null)
                                continue;

                            if (!LivingHasEffect(icb.Body, spell))
                            {
                                target = icb.Body;
                                break;
                            }
                        }
                    }

                    break;
                }

                #endregion
            }

            return target;
        }

        /// <summary>
        /// Add living to the aggrolist
        /// aggroAmount can be negative to lower amount of aggro
        /// </summary>
        public override void AddToAggroList(GameLiving living, long aggroAmount) { }

        public override void  RemoveFromAggroList(GameLiving living) { }

        /// <summary>
        /// Returns the best target to attack
        /// </summary>
        /// <returns>the best target</returns>
        protected override GameLiving CalculateNextAttackTarget()
        {
            return null;
        }

        /// <summary>
        /// Selects and attacks the next target or does nothing
        /// </summary>
        public override void AttackMostWanted() { }

        /// <summary>
        /// Owner attacked event
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        public override void OnOwnerAttacked(AttackData ad) { }

        #endregion
    }
}
