using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Players;
using Core.GS.Server;

namespace Core.GS;

public class GuardHealer : GameKeepGuard
{
	protected override IPlayerClass GetClass()
	{
		if (ModelRealm == ERealm.Albion) return new ClassCleric();
		else if (ModelRealm == ERealm.Midgard) return new ClassHealer();
		else if (ModelRealm == ERealm.Hibernia) return new ClassDruid();
		return new DefaultPlayerClass();
	}

	protected override void SetBlockEvadeParryChance()
	{
		base.SetBlockEvadeParryChance();
		BlockChance = 5;
	}

	protected override KeepGuardBrain GetBrain() => new GuardHealerBrain();

	protected override void SetName()
	{
		switch (ModelRealm)
		{
			case ERealm.None:
			case ERealm.Albion:
				Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Cleric");
				break;
			case ERealm.Midgard:
				Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Healer");
				break;
			case ERealm.Hibernia:
				Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Druid");
				break;
		}

		if (Realm == ERealm.None)
		{
			Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
		}
	}
}