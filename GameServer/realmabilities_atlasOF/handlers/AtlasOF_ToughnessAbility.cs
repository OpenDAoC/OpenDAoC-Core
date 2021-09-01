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
		
		public override int CostForUpgrade(int level) {	return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

		public override int GetAmountForLevel(int level) {
			
			// TODO : is this the only way we can access player in here ?
			//GamePlayer player = base.m_activeLiving as GamePlayer;
			log.WarnFormat("INSIDE GetAmountForLevel and current level to respond is {0}", level);
			
			if (level < 1) return 0;
			
			return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level);
			

			/*
			
			switch (level)
			{
				case 1: return eProperty.MaxHealth * (3 / 100);
				case 2: return eProperty.MaxHealth * (6 / 100);
				case 3: return eProperty.MaxHealth * (9 / 100);
				case 4: return eProperty.MaxHealth * (12 / 100);
				case 5: return eProperty.MaxHealth * (15 / 100);
				default: return 0;
			}
			*/
		}


		public override void Activate(GameLiving living, bool sendUpdates)
		{
			base.Activate(living, sendUpdates);

			// force UI to refresh encumberence value
			if (living is GamePlayer player) { log.Warn("INSIDE Activate"); player.Out.SendCharStatsUpdate(); }

		}

		public override void Deactivate(GameLiving living, bool sendUpdates)
		{
			base.Deactivate(living, sendUpdates);

			// force UI to refresh encumberence value
			if (living is GamePlayer player) { log.Warn("INSIDE Deactivate"); player.Out.SendCharStatsUpdate(); }
		}

		public override void OnLevelChange(int oldLevel, int newLevel = 0)
		{
			newLevel = oldLevel + 1;
			
			log.WarnFormat("INSIDE OnLevelChange with oldLevel {0} and newLevel {1}", oldLevel, newLevel);

			// force UI to refresh encumberence value
			if (base.m_activeLiving is GamePlayer player) { 
				log.WarnFormat("INSIDE OnLevelChange MATH with level {0} getAmmount {1} and value to add {2}", newLevel, GetAmountForLevel(newLevel), (player.MaxHealth * GetAmountForLevel(newLevel)) / 100);
				log.WarnFormat("INSIDE OnLevelChange MATH with player.MaxHealth BEFORE {0}", player.MaxHealth);
				player.MaxHealth += (player.MaxHealth * GetAmountForLevel(oldLevel)) / 100;
				SendUpdates(player);
				log.WarnFormat("INSIDE OnLevelChange MATH with player.MaxHealth AFTER {0}", player.MaxHealth);
				player.Out.SendCharStatsUpdate(); 
			}

			//base.OnLevelChange(oldLevel, newLevel);
		}




	}
}
