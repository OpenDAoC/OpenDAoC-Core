using Core.GS.ECS;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.AI.Brains
{
    public class CasterGuardBrain : KeepGuardBrain
    {
        public const int ANIMATION_INTERVAL = 60 * 60 * 1000;

        private long _lastAnimationTick;

        public override void Think()
        {
            CheckForNuking();

            if (!Body.IsCasting)
                CheckForAnimation();

            base.Think();
        }

        private void CheckForNuking()
        {
            if (_keepGuardBody.CanUseRanged)
                _keepGuardBody.CheckForNuke();
        }

        private void CheckForAnimation()
        {
            if (_lastAnimationTick + ANIMATION_INTERVAL > GameLoop.GameLoopTime)
                return;

            _lastAnimationTick = GameLoop.GameLoopTime;

            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellCastAnimation(Body, 4321, 30);
                new AuxEcsGameTimer(player, new AuxEcsGameTimer.AuxECSTimerCallback(ShowEffect), 3000);
            }
        }


        public int ShowEffect(AuxEcsGameTimer timer)
        {
            if (!Body.IsAlive)
                return 0;

            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(Body, Body, 4321, 0, false, 1);

                if (Body.IsWithinRadius(player, WorldMgr.INFO_DISTANCE))
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardCaster.SkinsHardens", Body.Name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
            }

            return 0;
        }
    }
}
