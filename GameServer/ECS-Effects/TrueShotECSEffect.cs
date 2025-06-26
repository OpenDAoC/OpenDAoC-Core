using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;

namespace DOL.GS
{
    public class TrueShotECSGameEffect : ECSGameAbilityEffect
    {
        public override ushort Icon => 3004;
        public override string Name => "Trueshot";
        public override bool HasPositiveEffect => true;

        private TimedRealmAbility _ability;

        public TrueShotECSGameEffect(TimedRealmAbility ability, ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.TrueShot;
            _ability = ability;
            Start();
        }

        public override void OnStartEffect()
        {
            OwnerPlayer?.Out.SendMessage("You prepare a Trueshot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public void Cancel(bool disableAbility)
        {
            if (disableAbility)
                _ability?.DisableSkill(Owner);

            Stop();
        }
    }
}
