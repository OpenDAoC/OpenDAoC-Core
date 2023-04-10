using DOL.Database;
using DOL.GS.Styles;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_StyleTundra : StyleRealmAbility
    {
        public AtlasOF_StyleTundra(DBAbility ability, int level) : base(ability, level) { }

        protected override Style CreateStyle()
        {
            DBStyle tmpStyle = new()
            {
                Name = "Tundra",
                GrowthRate = 1.34,
                EnduranceCost = 0,
                BonusToHit = 15,
                BonusToDefense = 10,
                WeaponTypeRequirement = 1001, // Any weapon type.
                OpeningRequirementType = 0,
                OpeningRequirementValue = 0,
                AttackResultRequirement = 0,
                Icon = 1697,
                SpecKeyName = GlobalSpellsLines.Realm_Spells
            };

            return new Style(tmpStyle, DisableSkill);
        }
    }
}
