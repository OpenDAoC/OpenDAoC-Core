using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Second Wind, restores 100% endu
	/// </summary>
	public class AtlasOF_SecondWind : TimedRealmAbility
	{
		public AtlasOF_SecondWind(DBAbility dba, int level) : base(dba, level) { }

        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>

        public override int MaxLevel { get { return 1; } }

		public override int CostForUpgrade(int level) { return 10; }

		public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasAugConLevel(player, 3); }

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			int regged = living.ChangeEndurance(living, eEnduranceChangeType.Spell, living.MaxEndurance);

			SendCasterSpellEffectAndCastMessage(living, 7003, regged > 0);

			if (regged > 0) DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 900;		// 900 = 15 min
		}

        public override void AddEffectsInfo(IList<string> list)
        {
        	//TODO Translate
        	if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
        	{
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SecondWindAbility.AddEffectsInfo.Info1"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SecondWindAbility.AddEffectsInfo.Info4"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SecondWindAbility.AddEffectsInfo.Info5"));
        	}
        	else
        	{
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SecondWindAbility.AddEffectsInfo.Info1"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SecondWindAbility.AddEffectsInfo.Info4"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SecondWindAbility.AddEffectsInfo.Info5"));
        	}
        }

	}
}