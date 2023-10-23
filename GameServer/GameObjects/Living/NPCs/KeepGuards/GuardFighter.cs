using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Players;
using Core.GS.Server;

namespace Core.GS;

public class GuardFighter : GameKeepGuard
{
	protected override IPlayerClass GetClass()
	{
		if (ModelRealm == ERealm.Albion) return new ClassArmsman();
		else if (ModelRealm == ERealm.Midgard) return new ClassWarrior();
		else if (ModelRealm == ERealm.Hibernia) return new ClassHero();
		return new DefaultPlayerClass();
	}

	protected override void SetBlockEvadeParryChance()
	{
		base.SetBlockEvadeParryChance();
		BlockChance = 10;
		ParryChance = 10;

		if (ModelRealm != ERealm.Albion)
		{
			EvadeChance = 5;
			ParryChance = 5;
		}
	}

	protected override void SetName()
	{
		switch (ModelRealm)
		{
			case ERealm.None:
			case ERealm.Albion:
				if (IsPortalKeepGuard)
				{
					Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.KnightCommander");
				}
				else
				{
					if (Gender == EGender.Male)
						Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Armsman");
					else Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Armswoman");
				}
				break;
			case ERealm.Midgard:
				if (IsPortalKeepGuard)
					Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.NordicJarl");
				else Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Huscarl");
				break;
			case ERealm.Hibernia:
				if (IsPortalKeepGuard)
					Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Champion");
				else Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Guardian");
				break;
		}

		if (Realm == ERealm.None)
		{
			Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
		}
	}
}