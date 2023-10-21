using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.PlayerClass;
using Core.GS.ServerProperties;
using Core.Language;

namespace Core.GS.Keeps
{
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
