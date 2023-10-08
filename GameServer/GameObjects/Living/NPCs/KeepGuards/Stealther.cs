using DOL.GS.PlayerClass;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Keeps
{
	public class GuardStealther : GameKeepGuard
	{
        public GuardStealther() : base()
        {
            Flags = eFlags.STEALTH;
        }

		protected override ICharacterClass GetClass()
		{
			if (ModelRealm == ERealm.Albion) return new ClassInfiltrator();
			else if (ModelRealm == ERealm.Midgard) return new ClassShadowblade();
			else if (ModelRealm == ERealm.Hibernia) return new ClassNightshade();
			return new DefaultCharacterClass();
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
					Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Infiltrator");
					break;
				case ERealm.Midgard:
					Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Shadowblade");
					break;
				case ERealm.Hibernia:
					Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Nightshade");
					break;
			}

			if (Realm == ERealm.None)
			{
				Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
			}
		}
	}
}
