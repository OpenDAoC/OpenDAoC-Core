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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AtlasOF_EtherealBondAbility(DBAbility dba, int level) : base(dba, level, eProperty.MaxMana) { }

        protected override string ValueUnit { get { return "%"; } }

        public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasSerenityLevel(player, 2); }

        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

        public override void Activate(GameLiving living, bool sendUpdates)
        {
            log.Warn("inside Activate");
            log.WarnFormat("1 - inside Activate Current maxmana is {0}", living.MaxMana);
            base.Activate(living, sendUpdates);
            log.WarnFormat("2 - inside Activate Current maxmana is {0}", living.MaxMana);

            /*
            // Ethereal Bond : Increases maximum power by 3% per level of this ability.
            // Pre-Requisits : Serenity lvl 2
            AtlasOF_SerenityAbility raSerenity = living.GetAbility<AtlasOF_SerenityAbility>();
            if (raSerenity != null)
            {
                log.WarnFormat("Serenity level is {0}", raSerenity.Level);

                if (raSerenity.Level < 2)
                {
                    log.WarnFormat("Serenity level is {0} and pre-requisite needs Serenity to be at least lvl 2 so we CANNOT activate RA ability", raSerenity.Level);
                    return;
                }
                else
                {
                    log.WarnFormat("Current MaxMana is {0}", living.MaxMana);

                    AtlasOF_RAMaxManaEnhancer raMana = AtlasOF_RAMaxManaEnhancer(le)

                    log.WarnFormat("Current MaxMana is {0}", living.MaxMana);
                }

            }
            else
            {
                log.WarnFormat("The player {0} does not have Serenity Ability and its a pre-requisite to Ethereal Bond", living.Name);
                base.Deactivate(living, sendUpdates);
            }
            */
        }

        public override void OnLevelChange(int oldLevel, int newLevel = 0)
        {
            log.WarnFormat("1 - inside OnLevelChange, oldLevel {0}, newLevel {1}, Current maxmana is {2}", oldLevel, newLevel, base.m_activeLiving.MaxMana);
            base.OnLevelChange(oldLevel, newLevel);
            log.WarnFormat("1 - inside OnLevelChange, oldLevel {0}, newLevel {1}, Current maxmana is {2}", oldLevel, newLevel, base.m_activeLiving.MaxMana);
        }

    }
}
