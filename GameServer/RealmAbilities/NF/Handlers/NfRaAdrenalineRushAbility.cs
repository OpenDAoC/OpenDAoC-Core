using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.RealmAbilities
{
    public class NfRaAdrenalineRushAbility : TimedRealmAbility
    {
        int m_duration = 20000;
        int m_value = 100;

        public NfRaAdrenalineRushAbility(DbAbility dba, int level) : base(dba, level) { }

        public override void Execute(GameLiving living)
        {
            GamePlayer player = living as GamePlayer;
            if (CheckPreconditions(living, DEAD | SITTING | STEALTHED)) return;
			if (player.EffectList.CountOfType<NfRaAdrenalineRushEffect>() > 0)
            {
                player.Out.SendMessage("You already an effect of that type!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            }
            SendCasterSpellEffectAndCastMessage(living, 7002, true);
            if (player != null)
            {
                new NfRaAdrenalineRushEffect(m_duration, m_value).Start(living);
            }
            DisableSkill(living);
        }
        public override int GetReUseDelay(int level)
        {
        	if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
        	{
	            switch (level)
	            {
	                case 1: return 1200;
	                case 2: return 900;
	                case 3: return 600;
	                case 4: return 450;
	                case 5: return 300;
	            }       		        		
        	}
        	else
        	{
	            switch (level)
	            {
	                case 1: return 1200;
	                case 2: return 600;
	                case 3: return 300;
	            }       		
	        }

            return 600;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
        	// TODO translate
        	if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
        	{
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info1"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info2"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info3"));
	            list.Add("");
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info4"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info5"));
        	}
        	else
        	{
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info1"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info2"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info3"));
	            list.Add("");
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info4"));
	            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AdrenalineRushAbility.AddEffectsInfo.Info5"));
        	}

        }

    }
}
