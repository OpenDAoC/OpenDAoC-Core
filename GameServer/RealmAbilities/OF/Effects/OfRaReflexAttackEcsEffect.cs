using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class ReflexAttackECSEffect : EcsGameAbilityEffect
    {
        public ReflexAttackECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.ReflexAttack;
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
                    OwnerPlayer.Out.SendMessage("You begin automatically counter-attacking melee attacks!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                }
                else
                {
                    t_player.Out.SendMessage(OwnerPlayer.Name + " starts automatically counter-attacking melee attacks!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                }
                t_player.Out.SendSpellEffectAnimation(OwnerPlayer, OwnerPlayer, 7012, 0, false, 1);
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
                    OwnerPlayer.Out.SendMessage("You stop automatically counter-attacking melee attacks!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                }
                else
                {
                    t_player.Out.SendMessage(OwnerPlayer.Name + " stops automatically counter-attacking melee attacks!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                }
            }
        }
    }
}