using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Language;

namespace DOL.AI.Brain
{
    public class CasterBrain : KeepGuardBrain
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
                new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(ShowEffect), 3000);
            }
        }

        public int ShowEffect(ECSGameTimer timer)
        {
            if (!Body.IsAlive)
                return 0;

            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(Body, Body, 4321, 0, false, 1);

                if (Body.IsWithinRadius(player, WorldMgr.INFO_DISTANCE))
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardCaster.SkinsHardens", Body.Name), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }

            return 0;
        }
    }
}
