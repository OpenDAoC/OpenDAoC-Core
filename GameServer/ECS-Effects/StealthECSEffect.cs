using System.Collections.Generic;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class StealthECSGameEffect : ECSGameAbilityEffect
    {
        public StealthECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.Stealth;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon => 0x193;
        public override string Name => LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.StealthEffect.Name");
        public override bool HasPositiveEffect => true;

        public override void OnStartEffect()
        {
            OwnerPlayer.StartStealthUncoverAction();

            if (OwnerPlayer.ObjectState is GameObject.eObjectState.Active)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Stealth.NowHidden"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            OwnerPlayer.Out.SendPlayerModelTypeChange(OwnerPlayer, 3);

            if (OwnerPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedBuff))
            {
                foreach (var speedBuff in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff))
                {
                    EffectService.RequestDisableEffect(speedBuff);
                }
            }

            // Cancel pulse effects.
            List<ECSPulseEffect> effects = OwnerPlayer.effectListComponent.GetAllPulseEffects();

            for (int i = 0; i < effects.Count; i++)
                EffectService.RequestCancelConcEffect(effects[i]);

            OwnerPlayer.Sprint(false);

            foreach (GamePlayer player in OwnerPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player != OwnerPlayer && !player.CanDetect(OwnerPlayer))
                    player.Out.SendObjectDelete(OwnerPlayer);
            }

            StealthStateChanged();
        }

        public override void OnStopEffect()
        {
            OwnerPlayer.StopStealthUncoverAction();

            if (OwnerPlayer.ObjectState == GameObject.eObjectState.Active)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Stealth.NoLongerHidden"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            OwnerPlayer.Out.SendPlayerModelTypeChange(OwnerPlayer, 2);

            foreach (GamePlayer otherPlayer in OwnerPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (otherPlayer == OwnerPlayer)
                    continue;

                otherPlayer.Out.SendPlayerCreate(OwnerPlayer);
                otherPlayer.Out.SendLivingEquipmentUpdate(OwnerPlayer);
            }

            if (OwnerPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedBuff))
            {
                var speedBuff = OwnerPlayer.effectListComponent.GetBestDisabledSpellEffect(eEffect.MovementSpeedBuff);

                if (speedBuff != null)
                {
                    speedBuff.IsBuffActive = false;
                    EffectService.RequestEnableEffect(speedBuff);
                }
            }

            EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(OwnerPlayer, eEffect.Vanish));
            EffectService.RequestCancelEffect(EffectListService.GetEffectOnTarget(OwnerPlayer, eEffect.Camouflage));
            StealthStateChanged();
        }

        private void StealthStateChanged()
        {
            OwnerPlayer.Notify(GamePlayerEvent.StealthStateChanged, OwnerPlayer, null);
            OwnerPlayer.OnMaxSpeedChange();
        }
    }
}
