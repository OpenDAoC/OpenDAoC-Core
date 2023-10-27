using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Players;
using Core.GS.Server;

namespace Core.GS;

public class GuardStealther : GameKeepGuard
{
    public GuardStealther() : base()
    {
        Flags = ENpcFlags.STEALTH;
    }

	protected override IPlayerClass GetClass()
	{
		if (ModelRealm == ERealm.Albion) return new ClassInfiltrator();
		else if (ModelRealm == ERealm.Midgard) return new ClassShadowblade();
		else if (ModelRealm == ERealm.Hibernia) return new ClassNightshade();
		return new DefaultPlayerClass();
	}

	protected override void SetBlockEvadeParryChance()
	{
		base.SetBlockEvadeParryChance();
		EvadeChance = 30;
	}

	protected override void SetName()
	{
		switch (ModelRealm)
		{
			case ERealm.None:
			case ERealm.Albion:
				Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Infiltrator");
				break;
			case ERealm.Midgard:
				Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Shadowblade");
				break;
			case ERealm.Hibernia:
				Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Nightshade");
				break;
		}

		if (Realm == ERealm.None)
		{
			Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
		}
	}
}