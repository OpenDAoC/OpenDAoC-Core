using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Ethereal Bond : Increases maximum power by 3% per level of this ability.
    /// Pre-Requisits : Serenity lvl 2
    /// </summary>
    public class AtlasOF_EtherealBondAbility : RAPropertyEnhancer
    {

        public AtlasOF_EtherealBondAbility(DBAbility dba, int level) : base(dba, level, eProperty.Undefined) { }

        public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasSerenityLevel(player, 2); }

        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

        
        /*
        public override void Activate(GameLiving living, bool sendUpdates)
        {
            // Ethereal Bond : Increases maximum power by 3% per level of this ability.
            // Pre-Requisits : Serenity lvl 2
            AtlasOF_SerenityAbility ra = living.GetAbility<AtlasOF_SerenityAbility>();
            if (ra != null)
            {
                log.WarnFormat("Serenity level is {0}", ra.Level);
                if (ra.Level < 2)
                {
                    log.WarnFormat("Serenity level is {0} and pre-requisite needs Serenity to be at least lvl 2 so we CANNOT activate RA ability", ra.Level);
                    base.Deactivate(living, sendUpdates);
                }
                else
                {
                    log.WarnFormat("Serenity level is {0} and we can activate Ethereal Bond", ra.Level);
                    base.Activate(living, sendUpdates);
                }

            }
            else
            {
                log.WarnFormat("The player {0} does not have Serenity Ability and its a pre-requisite to Ethereal Bond", living.Name);
                base.Deactivate(living, sendUpdates);
            }
        }
        */
    }
}
