using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.Players;
using Core.GS.Server;

namespace Core.GS;

public class GuardCaster : GameKeepGuard
{
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        return base.GetArmorAbsorb(slot) - 0.05;
    }

    protected override IPlayerClass GetClass()
    {
        if (ModelRealm == ERealm.Albion)
            return new ClassWizard();
        else if (ModelRealm == ERealm.Midgard)
            return new ClassRunemaster();
        else if (ModelRealm == ERealm.Hibernia)
            return new ClassEldritch();

        return new DefaultPlayerClass();
    }

    protected override KeepGuardBrain GetBrain()
    {
        return new CasterGuardBrain();
    }

    protected override void SetName()
    {
        switch (ModelRealm)
        {
            case ERealm.None:
            case ERealm.Albion:
            {
                if (IsPortalKeepGuard)
                    Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.MasterWizard");
                else
                    Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Wizard");

                break;
            }
            case ERealm.Midgard:
            {
                if (IsPortalKeepGuard)
                    Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.MasterRunes");
                else
                    Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Runemaster");

                break;
            }
            case ERealm.Hibernia:
            {
                if (IsPortalKeepGuard)
                    Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.MasterEldritch");
                else
                    Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Eldritch");

                break;
            }
        }

        if (Realm == ERealm.None)
            Name = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
    }
}