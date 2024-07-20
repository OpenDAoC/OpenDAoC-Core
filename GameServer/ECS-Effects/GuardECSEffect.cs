using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;

namespace DOL.GS
{
    public class GuardECSGameEffect : ECSGameAbilityEffect
    {
        public GameLiving Source { get; }
        public GameLiving Target { get; }
        public GuardECSGameEffect PairedEffect { get; private set; }
        public override ushort Icon => Owner is GameNPC ? (ushort) 1001 : (ushort) 412;
        public override string Name
        {
            get
            {
                GamePlayer playerOwner = Owner as GamePlayer;

                return Source != null && Target != null
                    ? LanguageMgr.GetTranslation(playerOwner?.Client, "Effects.GuardEffect.GuardedByName", Target.GetName(0, false), Source.GetName(0, false))
                    : LanguageMgr.GetTranslation(playerOwner?.Client, "Effects.GuardEffect.Name");
            }
        }
        public override bool HasPositiveEffect => true;

        public GuardECSGameEffect(ECSGameEffectInitParams initParams, GameLiving source, GameLiving target) : base(initParams)
        {
            Source = source;
            Target = target;
            EffectType = eEffect.Guard;
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
                if (!Source.IsWithinRadius(Target, GuardAbilityHandler.GUARD_DISTANCE))
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.GuardEffect.YouAreNowGuardingYBut", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.GuardEffect.XIsNowGuardingYouBut", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.GuardEffect.YouAreNowGuardingY", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.GuardEffect.XIsNowGuardingYou", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                PairedEffect = new GuardECSGameEffect(new ECSGameEffectInitParams(Target, 0, 1, null), Source, Target);
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
                playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.GuardEffect.YourNoLongerGuardingY", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.GuardEffect.XNoLongerGuardingYoy", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            EffectService.RequestImmediateCancelEffect(PairedEffect);
            base.OnStopEffect();
        }
    }
}
