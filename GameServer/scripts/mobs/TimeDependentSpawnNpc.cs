using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
    public abstract class TimeDependentSpawnNpc : HideableNpc
    {
        protected TimeDependentSpawnBrain TimeDependentBrain => Brain as TimeDependentSpawnBrain;

        public TimeDependentSpawnNpc(TimeDependentSpawnBrain brain) : base(brain) { }

        protected override int RespawnTimerCallback(ECSGameTimer respawnTimer)
        {
            // Ideally this should be done in AddToWorld, but WorldMgr isn't initialized when NPCs are created during server start up.
            CheckVisibility();
            return base.RespawnTimerCallback(respawnTimer);
        }

        public void CheckVisibility()
        {
            SetHidden(!ShouldBeVisible());
        }

        protected abstract bool ShouldBeVisible();
    }

    public class DaySpawn : TimeDependentSpawnNpc
    {
        public DaySpawn() : base(new TimeDependentSpawnBrain()) { }

        protected override bool ShouldBeVisible()
        {
            return !CurrentRegion.IsNightTime;
        }
    }

    public class NightSpawn : TimeDependentSpawnNpc
    {
        public NightSpawn() : base(new TimeDependentSpawnBrain()) { }

        protected override bool ShouldBeVisible()
        {
            return CurrentRegion.IsNightTime;
        }
    }
}

namespace DOL.AI.Brain
{
    public class TimeDependentSpawnBrain : StandardMobBrain
    {
        public override void Think()
        {
            if (!Body.InCombat)
                (Body as TimeDependentSpawnNpc)?.CheckVisibility();

            base.Think();
        }
    }
}
