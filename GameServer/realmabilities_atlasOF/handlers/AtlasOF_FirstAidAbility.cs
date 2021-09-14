using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Minor % based Self Heal -  30 / lvl
	/// </summary>
	public class AtlasOF_FirstAidAbility : TimedRealmAbility
	{

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public AtlasOF_FirstAidAbility(DBAbility dba, int level) : base(dba, level) { }

		
        public override int CostForUpgrade(int level)
        {
			if (level < 1) return 0;

			// TODO: CostForUpgrade it's not overriden !!! is always 3/6/9 no matter we override in here. this must be checked !!!
			switch (level)
            {
				case 1:
					return 3;
				case 2:
					return 6;
				case 3:
					return 10;
                default:
					return 10;
            }
        }

		
		
        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED | INCOMBAT)) return;

			int healAmount = 0;

			int currentLevelAbility = living.GetAbility<AtlasOF_FirstAidAbility>().Level;
			int currentCharMaxHealth = living.MaxHealth;
			
			// Minor % based Self Heal -  30 / lvl
			healAmount = ((currentLevelAbility * 30) * currentCharMaxHealth) / 100;
			log.InfoFormat("healAmount is {0}", healAmount);

			int healed = living.ChangeHealth(living, eHealthChangeType.Spell, healAmount);

			SendCasterSpellEffectAndCastMessage(living, 7001, healed > 0);

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				if (healed > 0) player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FirstAidAbility.Execute.HealYourself", healed), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				if (healAmount > healed)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FirstAidAbility.Execute.FullyHealed"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
			}
			if (healed > 0) DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 900;	// 15 min
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info1"));
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info2"));
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info3"));
			list.Add("");
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info4"));
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info5"));
			list.Add("");
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info6"));
			
		}
	}
}