using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;

namespace DOL.GS
{
    public class ProtectECSGameEffect : ECSGameAbilityEffect
    {
        public GameLiving Source { get; }
        public GameLiving Target { get; }
        public ProtectECSGameEffect PairedEffect { get; private set; }
        public override ushort Icon => 411;
        public override string Name
        {
            get
            {
                GamePlayer playerOwner = Owner as GamePlayer;

                return Source != null && Target != null
                    ? LanguageMgr.GetTranslation(playerOwner?.Client, "Effects.ProtectEffect.ProtectByName", Target.GetName(0, false), Source.GetName(0, false))
                    : LanguageMgr.GetTranslation(playerOwner?.Client, "Effects.ProtectEffect.Name");
            }
        }
        public override bool HasPositiveEffect => true;

        public ProtectECSGameEffect(ECSGameEffectInitParams initParams, GameLiving source, GameLiving target) : base(initParams)
        {
            Source = source;
            Target = target;
            EffectType = eEffect.Protect;
            EffectService.RequestStartEffect(this);
        }

        public override void OnStartEffect()
        {
            if (Source == null || Target == null)
                return;

            GamePlayer playerSource = Source as GamePlayer;
            GamePlayer playerTarget = Target as GamePlayer;

            if (playerSource != null && playerTarget != null)
            {
                Group group = playerSource.Group;

                if (group == null)
                    return;

                if (group != playerTarget.Group)
                    return;
            }

            if (Owner == Source)
            {
                if (!Source.IsWithinRadius(Target, ProtectAbilityHandler.PROTECT_DISTANCE))
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.ProtectEffect.YouProtectingYBut", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.ProtectEffect.XProtectingYouBut", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.ProtectEffect.YouProtectingY", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.ProtectEffect.XProtectingYou", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                PairedEffect = new ProtectECSGameEffect(new ECSGameEffectInitParams(Target, 0, 1), Source, Target);
                PairedEffect.PairedEffect = this;
            }

            base.OnStartEffect();
        }

        public override void OnStopEffect()
        {
            if (Source == Owner)
            {
                GamePlayer playerSource = Source as GamePlayer;
                GamePlayer playerTarget = Target as GamePlayer;
                playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.ProtectEffect.YouNoProtectY", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.ProtectEffect.XNoProtectYou", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            if (!PairedEffect.CancelEffect)
                EffectService.RequestImmediateCancelEffect(PairedEffect);

            base.OnStopEffect();
        }
    }
}
