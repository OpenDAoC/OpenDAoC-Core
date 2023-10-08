using DOL.AI.Brain;
using DOL.GS.PlayerClass;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Keeps
{
	public class GuardHealer : GameKeepGuard
	{
		protected override ICharacterClass GetClass()
		{
			if (ModelRealm == ERealm.Albion) return new ClassCleric();
			else if (ModelRealm == ERealm.Midgard) return new ClassHealer();
			else if (ModelRealm == ERealm.Hibernia) return new ClassDruid();
			return new DefaultCharacterClass();
		}

		protected override void SetBlockEvadeParryChance()
		{
			base.SetBlockEvadeParryChance();
			BlockChance = 5;
		}

		protected override KeepGuardBrain GetBrain() => new HealerGuardBrain();

		protected override void SetName()
		{
			switch (ModelRealm)
			{
				case ERealm.None:
				case ERealm.Albion:
					Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Cleric");
					break;
				case ERealm.Midgard:
					Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Healer");
					break;
				case ERealm.Hibernia:
					Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Druid");
					break;
			}

			if (Realm == ERealm.None)
			{
				Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
			}
		}
	}
}
