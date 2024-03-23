using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;

namespace DOL.GS
{
    public class InterceptECSGameEffect : ECSGameAbilityEffect
    {
        public GameLiving Source { get; }
        public GameLiving Target { get; }
        public InterceptECSGameEffect PairedEffect { get; private set; }
        public int InterceptChance
        {
            get
            {
                if (Source is not GameSummonedPet pet)
                    return 50;

                if (pet.Brain is BrittleBrain)
                    return 100;

                // Patch 1.123: The intercept chance on the Fossil Defender has been reduced by 20%.
                // Can't find documentation for previous intercept chance, so assuming 50%
                if (pet is BDSubPet)
                    return 30;

                // Patch 1.125: Reduced the spirit warrior's intercept chance from 75% to 60% and intercept radius from 150 to 125
                return 75;
            }
        }
        public override ushort Icon => Owner is GameNPC ? (ushort) 7249 : (ushort) 410;
        public override string Name
        {
            get
            {
                if (Owner is GamePlayer player)
                {
                    return Source != null && Target != null
                        ? LanguageMgr.GetTranslation(player.Client, "Effects.InterceptEffect.InterceptedByName", Target.GetName(0, false), Source.GetName(0, false))
                        : LanguageMgr.GetTranslation(player.Client, "Effects.InterceptEffect.Name");
                }

                return string.Empty;
            }
        }
        public override bool HasPositiveEffect => true;

        public InterceptECSGameEffect(ECSGameEffectInitParams initParams, GameLiving source, GameLiving target) : base(initParams)
        {
            Source = source;
            Target = target;
            EffectType = eEffect.Intercept;
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
                if (!Source.IsWithinRadius(Target, InterceptAbilityHandler.INTERCEPT_DISTANCE))
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.InterceptEffect.YouAttemtInterceptYBut", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.InterceptEffect.XAttemtInterceptYouBut", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.InterceptEffect.YouAttemtInterceptY", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.InterceptEffect.XAttemptInterceptYou", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                PairedEffect = new InterceptECSGameEffect(new ECSGameEffectInitParams(Target, 0, 1), Source, Target);
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
                playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.InterceptEffect.YouNoAttemtInterceptY", Target.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.InterceptEffect.XNoAttemptInterceptYou", Source.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            EffectService.RequestCancelEffect(PairedEffect);
            base.OnStopEffect();
        }
    }
}
