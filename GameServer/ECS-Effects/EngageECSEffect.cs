using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class EngageECSGameEffect : ECSGameAbilityEffect
    {
        private bool _manualCancel;

        public GameLiving EngageTarget { get; set; }
        public override ushort Icon => 421;
        public override string Name
        {
            get
            {
                GamePlayer player = Owner as GamePlayer;

                if (EngageTarget != null)
                    return LanguageMgr.GetTranslation(player.Client, "Effects.EngageEffect.EngageName", EngageTarget.GetName(0, false));

                return LanguageMgr.GetTranslation(player.Client, "Effects.EngageEffect.Name");
            }
        }
        public override bool HasPositiveEffect => true;

        public EngageECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.Engage;
            EffectService.RequestStartEffect(this);
        }

        public override void OnStartEffect()
        {
            EngageTarget = Owner.TargetObject as GameLiving;
            Owner.IsEngaging = true;
            OwnerPlayer?.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.ConcOnBlockingX", EngageTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

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
            if (OwnerPlayer != null)
            {
                if (_manualCancel)
                    OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.YouNoConcOnBlock"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                else
                    OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.YouNoAttemptToEngageT"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            Owner.IsEngaging = false;

            if (Owner.TargetObject != null)
                Owner.attackComponent.RequestStartAttack();
            else
                Owner.attackComponent.StopAttack();
        }

        public void Cancel(bool manualCancel, bool startAttackAfterCancel)
        {
            _manualCancel = manualCancel;
            EffectService.RequestImmediateCancelEffect(this, manualCancel);
        }
    }
}
