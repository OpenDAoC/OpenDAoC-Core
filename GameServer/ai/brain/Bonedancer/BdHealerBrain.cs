using DOL.GS;
using DOL.GS.ServerProperties;

namespace DOL.AI.Brain
{
    public class BdHealerBrain : BdPetBrain
    {
        public BdHealerBrain(GameLiving owner) : base(owner)
        {
            AggroLevel = 0;
            AggroRange = 0;
        }

        public override eAggressionState AggressionState
        {
            get => eAggressionState.Passive;
            set { }
        }

        public override void Attack(GameObject target) { }

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

        public override void AddToAggroList(GameLiving living, long aggroAmount) { }

        public override void RemoveFromAggroList(GameLiving living) { }

        protected override GameLiving CalculateNextAttackTarget()
        {
            return null;
        }

        public override void AttackMostWanted() { }

        public override void OnOwnerAttacked(AttackData ad) { }
    }
}
