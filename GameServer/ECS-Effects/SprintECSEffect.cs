using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class SprintECSGameEffect : ECSGameAbilityEffect
    {
        public SprintECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams) 
        {
            EffectType = eEffect.Sprint;
            NextTick = GameLoop.GameLoopTime + 1;
            PulseFreq = 200;
            EffectService.RequestStartEffect(this);
        }

        private int _idleTicks = 0;

        public override ushort Icon => 0x199;
        public override string Name => LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.SprintEffect.Name");
        public override bool HasPositiveEffect => true;

        public override long GetRemainingTimeForClient()
        {
            return 1000;
        }

        public override void OnStartEffect()
        {
            if (OwnerPlayer != null)
            {
                int regen = OwnerPlayer.GetModified(eProperty.EnduranceRegenerationAmount);
                var enduranceChant = OwnerPlayer.GetModified(eProperty.FatigueConsumption);
                var cost = -5 + regen;

                if (enduranceChant > 1)
                    cost = (int) Math.Ceiling(cost * enduranceChant * 0.01);

                OwnerPlayer.Endurance += cost;
                OwnerPlayer.Out.SendUpdateMaxSpeed();
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Sprint.PrepareSprint"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Owner.StartEnduranceRegeneration();
            }
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer != null)
            {
                OwnerPlayer.Out.SendUpdateMaxSpeed();
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Sprint.NoLongerReady"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public override void OnEffectPulse()
        {
            if (Owner.IsMoving)
                _idleTicks = 0;
            else
                _idleTicks++;

            if (Owner.Endurance - 5 <= 0 || _idleTicks >= 30)
                EffectService.RequestImmediateCancelEffect(this);
        }
    }
}
