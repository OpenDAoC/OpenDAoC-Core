using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Toughness : Increases maximum hit points by 3% per level of this ability.
    /// </summary>
    public class AtlasOF_ToughnessAbility : RAPropertyEnhancer
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AtlasOF_ToughnessAbility(DBAbility dba, int level) : base(dba, level, eProperty.MaxHealth) { }

        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level); }

        public override void Activate(GameLiving living, bool sendUpdates)
        {
            log.Warn("INSIDE Activate");
            base.Activate(living, sendUpdates);
        }

        public override void Deactivate(GameLiving living, bool sendUpdates)
        {
            log.Warn("INSIDE Deactivate");
            base.Deactivate(living, sendUpdates);
        }

        public override void OnLevelChange(int oldLevel, int newLevel = 0)
        {
            log.Warn("INSIDE onLevelChange");
            SendUpdates(this.m_activeLiving);
            base.OnLevelChange(oldLevel, newLevel);
        }
    }
}
