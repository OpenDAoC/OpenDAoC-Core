using Core.Database.Tables;
using Core.GS.Styles;

namespace Core.GS.RealmAbilities;

public class OfRaTundraStyle : StyleRealmAbility
{
    public OfRaTundraStyle(DbAbility ability, int level) : base(ability, level) { }

    protected override Style CreateStyle()
    {
        DbStyle tmpStyle = new()
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