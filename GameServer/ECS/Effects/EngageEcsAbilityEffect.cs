using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.ECS;

public class EngageEcsAbilityEffect : EcsGameAbilityEffect
{
    private bool _startAttackAfterCancel;

    public GameLiving EngageTarget { get; set; }
    public override ushort Icon => 421;
    public override string Name
    {
        get
        {
            if (EngageTarget != null)
                return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.EngageEffect.EngageName", EngageTarget.GetName(0, false));

            return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.EngageEffect.Name");
        }
    }
    public override bool HasPositiveEffect => true;

    public EngageEcsAbilityEffect(EcsGameEffectInitParams initParams) : base(initParams)
    {
        EffectType = EEffect.Engage;
        EffectService.RequestStartEffect(this);
    }

    public override void OnStartEffect()
    {
        EngageTarget = Owner.TargetObject as GameLiving;
        Owner.IsEngaging = true;

        if (OwnerPlayer != null)
            OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.ConcOnBlockingX", EngageTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);

        // Only emulate attack mode so it works more like on live servers.
        // Entering real attack mode while engaging someone stops engage.
        // Other players will see attack mode after pos update packet is sent.
        if (!Owner.attackComponent.AttackState)
        {
            Owner.attackComponent.AttackState = true;

            if (Owner is GamePlayer playerOwner)
                playerOwner.Out.SendAttackMode(true);
        }
    }

    public override void OnStopEffect()
    {
        Owner.IsEngaging = false;

        if (_startAttackAfterCancel)
            Owner.attackComponent.RequestStartAttack(Owner.TargetObject);
    }

    public void Cancel(bool manualCancel, bool startAttackAfterCancel)
    {
        _startAttackAfterCancel = startAttackAfterCancel;
        EffectService.RequestImmediateCancelEffect(this, manualCancel);


        if (OwnerPlayer != null)
        {
            if (manualCancel)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.YouNoConcOnBlock"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            else
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.YouNoAttemptToEngageT"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
        }
    }
}