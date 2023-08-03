using DOL.GS.PlayerClass;
using DOL.Language;

namespace DOL.GS.Keeps
{
	public class GuardArcher : GameKeepGuard
	{
		protected override IPlayerClass GetClass()
		{
			if (ModelRealm == ERealm.Albion) return new ClassScout();
			else if (ModelRealm == ERealm.Midgard) return new ClassHunter();
			else if (ModelRealm == ERealm.Hibernia) return new ClassRanger();
			return new DefaultPlayerClass();
		}

		protected override void SetBlockEvadeParryChance()
		{
			base.SetBlockEvadeParryChance();
			if (ModelRealm == ERealm.Albion)
			{
				BlockChance = 10;
				EvadeChance = 5;
			}
			else
			{
				EvadeChance = 15;
			}

		}

		protected override void SetName()
		{
			switch (ModelRealm)
			{
				case ERealm.None:
				case ERealm.Albion:
					if (IsPortalKeepGuard)
						Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.BowmanCommander");
					else Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.Scout");
					break;
				case ERealm.Midgard:
					if (IsPortalKeepGuard)
						Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.NordicHunter");
					else Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.Hunter");
					break;
				case ERealm.Hibernia:
					if (IsPortalKeepGuard)
						Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.MasterRanger");
					else Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.Ranger");
					break;
			}

			if (Realm == ERealm.None)
			{
				Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
			}
		}
	}
}