using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;
using DOL.GS.Spells;
using DOL.GS.Styles;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_StyleTundra : StyleRealmAbility
    {
        
        public AtlasOF_StyleTundra(DBAbility ability, int level) : base(ability, level)
        {
        }

        protected override Style CreateStyle()
        {
            DBStyle tmpStyle = new DBStyle();
            tmpStyle.Name = "Tundra";
            tmpStyle.GrowthRate = 1.34;
            tmpStyle.EnduranceCost = 0;
            tmpStyle.BonusToHit = 15;
            tmpStyle.BonusToDefense = 10;
            tmpStyle.WeaponTypeRequirement = 1001; //any weapon type
            tmpStyle.OpeningRequirementType = 0;
            tmpStyle.OpeningRequirementValue = 0;
            tmpStyle.AttackResultRequirement = 0;
            tmpStyle.Icon = 1697; 
            tmpStyle.SpecKeyName = GlobalSpellsLines.Realm_Spells;
            return new Style(tmpStyle);
        }
    }

}
