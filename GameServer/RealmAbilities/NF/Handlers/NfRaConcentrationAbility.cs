using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Languages;

namespace Core.GS.RealmAbilities;

public class NfRaConcentrationAbility : TimedRealmAbility
{
	public NfRaConcentrationAbility(DbAbility dba, int level) : base(dba, level) { }

    /// <summary>
    /// Action
    /// </summary>
    /// <param name="living"></param>
    public override void Execute(GameLiving living)
    {
        if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

        SendCasterSpellEffectAndCastMessage(living, 7006, true);

        GamePlayer player = living as GamePlayer;
        if (player != null)
        {
            player.RemoveDisabledSkill(SkillBase.GetAbility(Abilities.Quickcast));
        }
        DisableSkill(living);
    }

    public override int GetReUseDelay(int level)
	{
		if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
		{
			switch (level)
			{
				case 1: return 900;
				case 2: return 540;
				case 3: return 180;
				case 4: return 90;
				case 5: return 30;
			}
		}
		else
		{
			switch (level)
			{
				case 1: return 15 * 60;
				case 2: return 3 * 60;
				case 3: return 30;
			}
		}

		return 0;
	}

	public override void AddEffectsInfo(IList<string> list)
	{
		/*
		//TODO Translate
		if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
		{
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info1"));
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info2"));
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info3"));
            list.Add("");
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info4"));
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info5"));			}
		else
		{
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info1"));
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info2"));
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info3"));
            list.Add("");
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info4"));
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info5"));
		}*/
		
		list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info4"));
		list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "ConcentrationAbility.AddEffectsInfo.Info5"));
	}
}