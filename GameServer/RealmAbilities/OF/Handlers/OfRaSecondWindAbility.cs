using System.Collections.Generic;
using Core.Database;
using Core.Language;

namespace Core.GS.RealmAbilities
{
	public class OfRaSecondWindAbility : TimedRealmAbility
	{
		public OfRaSecondWindAbility(DbAbility dba, int level) : base(dba, level) { }

        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>

        public override int MaxLevel { get { return 1; } }

		public override int CostForUpgrade(int level) { return 10; }

		public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugConLevel(player) >= 3; }

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			int regged = living.ChangeEndurance(living, EEnduranceChangeType.Spell, living.MaxEndurance);

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