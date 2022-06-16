using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class ReflexAttackECSEffect : ECSGameAbilityEffect
    {
        public ReflexAttackECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.ReflexAttack;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4277; } }
        public override string Name { get { return "Reflex Attack"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            foreach (GamePlayer t_player in OwnerPlayer.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (t_player == OwnerPlayer)
                {
                    OwnerPlayer.Out.SendMessage("You begin automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    t_player.Out.SendMessage(OwnerPlayer.Name + " starts automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                OwnerPlayer.Out.SendSpellEffectAnimation(OwnerPlayer, OwnerPlayer, 7012, 0, false, 1);
            }
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            foreach (GamePlayer t_player in OwnerPlayer.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (t_player == OwnerPlayer)
                {
                    OwnerPlayer.Out.SendMessage("You stop automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    t_player.Out.SendMessage(OwnerPlayer.Name + " stops automatically counter-attacking melee attacks!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
            }
        }
    }
}
