using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain that can be controlled
    /// </summary>
    public class BdBufferBrain : BdPetBrain
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs new controlled npc brain
        /// </summary>
        /// <param name="owner"></param>
        public BdBufferBrain(GameLiving owner) : base(owner) { }

        /// <summary>
        /// Attack the target on command
        /// </summary>
        /// <param name="target"></param>
        public override void Attack(GameObject target)
        {
            // Don't stop casting. Buffers should prioritize buffing.
            // 'AttackMostWanted()' will be called automatically once the pet is done buffing.
            if (m_orderAttackTarget == target)
                return;

            m_orderAttackTarget = target as GameLiving;
            FSM.SetCurrentState(eFSMStateType.AGGRO);
        }

        /// <summary>
        /// Checks the Abilities
        /// </summary>
        public override void CheckAbilities() { }

        protected override GameLiving FindTargetForDefensiveSpell(Spell spell)
        {
            GameLiving target = null;

            switch (spell.SpellType)
            {
                case eSpellType.CombatSpeedBuff:
                case eSpellType.DamageShield:
                case eSpellType.Bladeturn:
                {
                    if (Body.IsAttacking)
                        break;

                    //Buff self
                    if (!LivingHasEffect(Body, spell))
                    {
                        target = Body;
                        break;
                    }

                    if (spell.Target != eSpellTarget.SELF)
                    {
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

                            // Buff other minions.
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
                    }

                    break;
                }
            }

            return target;
        }
    }
}
