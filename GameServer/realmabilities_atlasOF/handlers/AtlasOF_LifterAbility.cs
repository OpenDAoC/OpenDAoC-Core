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
	/// Lifter : 20% additional maximum carrying capacity per level.
	/// </summary>
	public class AtlasOF_LifterAbility : RAPropertyEnhancer
    {

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// TODO: Chek if this should be changed to eProperty.Strength 
		public AtlasOF_LifterAbility(DBAbility dba, int level) : base(dba, level, eProperty.Undefined) { }
		
		protected override string ValueUnit { get {	return "%"; } }

		public override int CostForUpgrade(int level) {	return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			
			return level * 20;
		}

        public override void Activate(GameLiving living, bool sendUpdates)
        {
			base.Activate(living, sendUpdates);

			// force UI to refresh encumberence value
			if (living is GamePlayer player) { log.Warn("INSIDE Activate"); player.Out.SendEncumberance(); }

		}

		public override void Deactivate(GameLiving living, bool sendUpdates)
        {
			base.Deactivate(living, sendUpdates);

			// force UI to refresh encumberence value
			if (living is GamePlayer player) { log.Warn("INSIDE Deactivate"); player.Out.SendEncumberance(); }
		}

        public override void OnLevelChange(int oldLevel, int newLevel = 0) 
		{
			base.OnLevelChange(oldLevel, newLevel);

			// force UI to refresh encumberence value
			if (base.m_activeLiving is GamePlayer player) { log.Warn("INSIDE OnLevelChange"); player.Out.SendEncumberance(); }

		}
		
    }
}
