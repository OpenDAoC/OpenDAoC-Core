using DOL.Database;
using DOL.GS.Styles;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_StyleWinterMoon : StyleRealmAbility
    {
        public AtlasOF_StyleWinterMoon(DbAbility ability, int level) : base(ability, level) { }

        protected override Style CreateStyle()
        {
            DbStyle tmpStyle = new()
            {
                Name = "Winter Moon",
                GrowthRate = 1.4,
                EnduranceCost = 0,
                BonusToHit = 15,
                BonusToDefense = 10,
                WeaponTypeRequirement = 1001, // Any weapon type.
                OpeningRequirementType = 0,
                OpeningRequirementValue = 0,
                AttackResultRequirement = 0,
                Icon = 1698,
                SpecKeyName = GlobalSpellsLines.Realm_Spells
            };

            return new Style(tmpStyle, DisableSkill);
        }
    }
}
