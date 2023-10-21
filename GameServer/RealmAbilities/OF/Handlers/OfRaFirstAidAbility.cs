using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.RealmAbilities
{
	public class OfRaFirstAidAbility : NfRaFirstAidAbility
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public OfRaFirstAidAbility(DbAbility dba, int level) : base(dba, level) { }

		public override int MaxLevel { get { return 3; } }
        public override int CostForUpgrade(int currentLevel) { return OfRaHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }
		
        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED | INCOMBAT)) return;

			int healAmount = 0;

			int currentLevelAbility = living.GetAbility<OfRaFirstAidAbility>().Level;
			
			// int currentCharMaxHealth = living.MaxHealth;
			// Minor % based Self Heal -  30 / lvl
			// healAmount = ((currentLevelAbility * 30) * currentCharMaxHealth) / 100;
			
			// 300hp at Lv50 per ability level as per 1.65
			// scaled to player level
			GamePlayer player = living as GamePlayer;
			var scaleLevel = (double)player.Level / 50;
			healAmount = (int)(currentLevelAbility * 300 * scaleLevel);

			log.InfoFormat("healAmount is {0}", healAmount);

			int healed = living.ChangeHealth(living, EHealthChangeType.Spell, healAmount);

			SendCasterSpellEffectAndCastMessage(living, 7001, healed > 0);

			if (player != null)
			{
				if (healed > 0) player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FirstAidAbility.Execute.HealYourself", healed), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				if (healAmount > healed)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FirstAidAbility.Execute.FullyHealed"), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				}
			}
			if (healed > 0) DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 900;	// 900 = 15 min / 1800 = 30 min
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info1"));
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info2"));
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info3"));
			list.Add("");
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info4"));
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info5"));

		}
	}
}