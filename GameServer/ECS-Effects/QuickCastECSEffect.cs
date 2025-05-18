using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class QuickCastECSGameEffect : ECSGameAbilityEffect
    {
        public QuickCastECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.QuickCast;
            Start();
        }

        public const int DURATION = 3000;

        public override ushort Icon => 0x0190;
        public override string Name => LanguageMgr.GetTranslation(((GamePlayer) Owner).Client, "Effects.QuickCastEffect.Name");
        public override bool HasPositiveEffect => true;

        public override long GetRemainingTimeForClient()
        {
            return 0;
        }

        public override void OnStartEffect()
        {
            if (Owner is GamePlayer player)
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Effects.QuickCastEffect.YouActivatedQC"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            Owner.TempProperties.RemoveProperty(Spells.SpellHandler.INTERRUPT_TIMEOUT_PROPERTY);
        }

        public override void OnStopEffect() { }

        public void Cancel(bool playerCancel)
        {
            if (playerCancel)
            {
                if (Owner is GamePlayer player)
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Effects.QuickCastEffect.YourNextSpellNoQCed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            Stop(playerCancel);
        }
    }
}
