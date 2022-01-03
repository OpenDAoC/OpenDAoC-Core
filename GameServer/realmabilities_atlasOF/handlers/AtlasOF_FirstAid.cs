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
	public class AtlasOF_FirstAid : TimedRealmAbility
	{

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public AtlasOF_FirstAid(DBAbility dba, int level) : base(dba, level) { }

		public override int MaxLevel { get { return 3; } }

		public override int CostForUpgrade(int level)
        {
			switch (level)
            {
				case 1:
					return 3;
				case 2:
					return 6;
				case 3:
					return 10;
                default:	// default must return value for lvl 1
					return 3;
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

			int currentLevelAbility = living.GetAbility<AtlasOF_FirstAid>().Level;
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
			list.Add("");
			list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "FirstAidAbility.AddEffectsInfo.Info6"));
			
		}
	}
}