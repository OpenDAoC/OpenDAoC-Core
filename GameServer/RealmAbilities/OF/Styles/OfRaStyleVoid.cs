using DOL.Database;
using DOL.GS.Styles;

namespace DOL.GS.RealmAbilities
{
    public class OfRaStyleVoid : StyleRealmAbility
    {
        public OfRaStyleVoid(DbAbilities ability, int level) : base(ability, level) { }

        protected override Style CreateStyle()
        {
            DBStyle tmpStyle = new()
            {
                Name = "Void",
                GrowthRate = 1.4,
                EnduranceCost = 0,
                BonusToHit = 15,
                BonusToDefense = 10,
                WeaponTypeRequirement = 1001, // Any weapon type.
                OpeningRequirementType = 0,
                OpeningRequirementValue = 0,
                AttackResultRequirement = 0,
                Icon = 1695,
                SpecKeyName = GlobalSpellsLines.Realm_Spells
            };

            return new Style(tmpStyle, DisableSkill);
        }
    }
}