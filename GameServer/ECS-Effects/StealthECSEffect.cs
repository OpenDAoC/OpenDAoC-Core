using DOL.Language;
using DOL.GS.PacketHandler;
using DOL.Events;

namespace DOL.GS
{
    public class StealthECSGameEffect : ECSGameAbilityEffect
    {
        public StealthECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Stealth;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 0x193; } }
        public override string Name { get { return LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.StealthEffect.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {           
            OwnerPlayer.StartStealthUncoverAction();

            if (OwnerPlayer.ObjectState == GameObject.eObjectState.Active)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Stealth.NowHidden"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            OwnerPlayer.Out.SendPlayerModelTypeChange(OwnerPlayer, 3);

            if (OwnerPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedBuff))
            {
                foreach (var speedBuff in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff))
                {
                    EffectService.RequestDisableEffect(speedBuff);
                }
            }
            // Cancel pulse effect
            if (OwnerPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Pulse))
            {
                EffectService.RequestImmediateCancelConcEffect(EffectListService.GetPulseEffectOnTarget(OwnerPlayer));
            }

            OwnerPlayer.Sprint(false);

            if (OwnerPlayer.Client.Account.PrivLevel == 1 || OwnerPlayer.Client.Account.PrivLevel == 0)
            {
                //GameEventMgr.AddHandler(this, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(Unstealth));
                foreach (GamePlayer player in OwnerPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null || player == OwnerPlayer) continue;
                    if (!player.CanDetect(OwnerPlayer))
                        player.Out.SendObjectDelete(OwnerPlayer);
                }
                OwnerPlayer.Out.SendUpdateMaxSpeed();
            }

            StealthStateChanged();
        }

        public override void OnStopEffect()
        {
            OwnerPlayer.StopStealthUncoverAction();

            if (OwnerPlayer.ObjectState == GameObject.eObjectState.Active)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Stealth.NoLongerHidden"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            OwnerPlayer.Out.SendPlayerModelTypeChange(OwnerPlayer, 2);

            //GameEventMgr.RemoveHandler(this, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(GamePlayer.Unstealth));
            foreach (GamePlayer otherPlayer in OwnerPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (otherPlayer == null || otherPlayer == OwnerPlayer) continue;

                /// [Atlas - Takii] This commented code from DOL causes a large (1-2 seconds) delay before the target unstealths.
                /// It does not seem to cause any issues related to targeting despite the comments.
                //if a player could see us stealthed, we just update our model to avoid untargetting.
                // 					if (player.CanDetect(this))
                // 						player.Out.SendPlayerModelTypeChange(this, 2);
                // 					else
                // 						player.Out.SendPlayerCreate(this);
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

            StealthStateChanged();

            // This needs to be restored if we have the Camouflage ability on this server.
            //             if (Owner.HasAbility(Abilities.Camouflage))
            //             {
            //                 IGameEffect camouflage = m_player.EffectList.GetOfType<CamouflageEffect>();
            //                 if (camouflage != null)
            //                     camouflage.Cancel(false);
            //             }
        }

        private void StealthStateChanged()
        {
            OwnerPlayer.Notify(GamePlayerEvent.StealthStateChanged, OwnerPlayer, null);
            if (OwnerPlayer.Client.Account.PrivLevel == 1 || OwnerPlayer.Client.Account.PrivLevel == 0)
            {
                OwnerPlayer.Out.SendUpdateMaxSpeed();
            }
        }
    }
}