using DOL.GS;

namespace DOL.AI.Brain
{
    public class BdBufferBrain : BdPetBrain
    {
        public BdBufferBrain(GameLiving owner) : base(owner) { }

        public override void Think()
        {
            if (base.CheckSpells(eCheckSpellType.Defensive))
                Body.StopAttack();
            else
                base.Think();
        }
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
