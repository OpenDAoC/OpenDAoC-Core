using DOL.GS;
using DOL.GS.RealmAbilities;

namespace DOL.AI.Brain
{
    public class BdMeleeBrain : BdPetBrain
    {
        public BdMeleeBrain(GameLiving owner) : base(owner) { }

        public override void CheckAbilities()
        {
            if (Body.Abilities == null || Body.Abilities.Count <= 0)
                return;

            foreach (Ability ab in Body.Abilities.Values)
            {
                switch (ab.KeyName)
                {
                    case Abilities.ChargeAbility:
                    {
                        if (Body.TargetObject != null && !Body.IsWithinRadius(Body.TargetObject, 500))
                        {
                            ChargeAbility charge = Body.GetAbility<ChargeAbility>();

                            if (charge != null && Body.GetSkillDisabledDuration(charge) <= 0)
                                charge.Execute(Body);
                        }

                        break;
                    }
                }
            }
        }

        public override bool CheckSpells(eCheckSpellType type) { return false; }
    }
}
