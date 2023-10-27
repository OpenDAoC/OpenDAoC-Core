using System.Collections;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS.RealmAbilities;

public class NfRaDashingDefenseAbility : TimedRealmAbility
{
    public NfRaDashingDefenseAbility(DbAbility dba, int level) : base(dba, level) { }

    public const string Dashing = "Dashing";

    //private RegionTimer m_expireTimer;
    int m_duration = 1;
    int m_range = 1000;
    //private GamePlayer m_player;

    public override void Execute(GameLiving living)
    {
        GamePlayer player = living as GamePlayer;
        if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
        if (player.TempProperties.GetProperty(Dashing, false))
        {
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DashingDefenseAbility.Execute.AlreadyEffect"), EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
			return;
        }
		
        if(ServerProperty.USE_NEW_ACTIVES_RAS_SCALING)
        {
            switch (Level)
            {
                case 1: m_duration = 10; break;
                case 2: m_duration = 20; break;
                case 3: m_duration = 30; break;
                case 4: m_duration = 45; break;
                case 5: m_duration = 60; break;
                default: return;
            }            	           	
        }
        else
        {
            switch (Level)
            {
                case 1: m_duration = 10; break;
                case 2: m_duration = 30; break;
                case 3: m_duration = 60; break;
                default: return;
            }            	
        }

        DisableSkill(living);

        ArrayList targets = new ArrayList();
        if (player.Group == null)
            {
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DashingDefenseAbility.Execute.MustInGroup"), EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return;
            }
        else foreach (GamePlayer grpMate in player.Group.GetPlayersInTheGroup())
                if (player.IsWithinRadius(grpMate, m_range) && grpMate.IsAlive)
                    targets.Add(grpMate);

        bool success;
        foreach (GamePlayer target in targets)
        {
            //send spelleffect
            if (!target.IsAlive) continue;
            success = !target.TempProperties.GetProperty(Dashing, false);
            if (success)
                if (target != null && target != player)
                {
                    new NfRaDashingDefenseEffect().Start(player, target, m_duration);
                }
        }

    }

    public override int GetReUseDelay(int level)
    {
        return 420;
    }

	public override void AddEffectsInfo(IList<string> list)
	{
		//TODO Translate
		if(ServerProperty.USE_NEW_ACTIVES_RAS_SCALING)
		{
			list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DashingDefenseAbility.AddEffectsInfo.Info1"));
			list.Add("");
			list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DashingDefenseAbility.AddEffectsInfo.Info2"));
			list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DashingDefenseAbility.AddEffectsInfo.Info3"));
		}
		else
		{
			list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DashingDefenseAbility.AddEffectsInfo.Info1"));
			list.Add("");
			list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DashingDefenseAbility.AddEffectsInfo.Info2"));
			list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DashingDefenseAbility.AddEffectsInfo.Info3"));
		}
	}
}