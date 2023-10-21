using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.ECS;

public class QuickCastEcsAbilityEffect : EcsGameAbilityEffect
{
    public QuickCastEcsAbilityEffect(EcsGameEffectInitParams initParams)
        : base(initParams)
    {
        EffectType = EEffect.QuickCast;
        EffectService.RequestStartEffect(this);
    }

    public const int DURATION = 3000;

    public override ushort Icon { get { return 0x0190; } }
    public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.QuickCastEffect.Name"); } }
    public override bool HasPositiveEffect { get { return true; } }
    public override long GetRemainingTimeForClient() { { return 0; } }

    public override void OnStartEffect()
    {
        if (Owner is GamePlayer)
            (Owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((Owner as GamePlayer).Client, "Effects.QuickCastEffect.YouActivatedQC"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
        Owner.TempProperties.RemoveProperty(Spells.SpellHandler.INTERRUPT_TIMEOUT_PROPERTY);
    }
    public override void OnStopEffect()
    {

    }
    public void Cancel(bool playerCancel)
    {
        if (playerCancel)
        {
            if (Owner is GamePlayer)
                (Owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((Owner as GamePlayer).Client, "Effects.QuickCastEffect.YourNextSpellNoQCed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
        }

        EffectService.RequestImmediateCancelEffect(this, playerCancel);
    }
}