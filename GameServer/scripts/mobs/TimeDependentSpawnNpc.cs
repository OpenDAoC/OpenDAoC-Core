using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
    public abstract class TimeDependentSpawnNpc : GameNPC
    {
        private const eFlags INACTIVE_FLAGS = eFlags.CANTTARGET | eFlags.DONTSHOWNAME | eFlags.PEACE;

        public TimeDependentSpawnBrain TimeDependentBrain => Brain as TimeDependentSpawnBrain;
        public override eFlags Flags => TimeDependentBrain == null || TimeDependentBrain.IsVisible ? base.Flags : base.Flags | INACTIVE_FLAGS;
        public override ushort Model => TimeDependentBrain == null || TimeDependentBrain.IsVisible ? base.Model : (ushort) 1;

        public TimeDependentSpawnNpc(TimeDependentSpawnBrain brain) : base(brain) { }
    }

    public class DaySpawn : TimeDependentSpawnNpc
    {
        public DaySpawn() : base(new DaySpawnBrain()) { }
    }

    public class NightSpawn : TimeDependentSpawnNpc
    {
        public NightSpawn() : base(new NightSpawnBrain()) { }
    }
}

namespace DOL.AI.Brain
{
    public abstract class TimeDependentSpawnBrain : StandardMobBrain
    {
        public bool IsVisible { get; private set; }

        public override void Think()
        {
            if (!Body.InCombat)
            {
                bool previousVisibility = IsVisible;
                IsVisible = ShouldBeVisible();

                if (previousVisibility != IsVisible)
                    ClientService.CreateObjectForPlayers(Body);
            }

            base.Think();
        }

        protected abstract bool ShouldBeVisible();
    }

    public class DaySpawnBrain : TimeDependentSpawnBrain
    {
        protected override bool ShouldBeVisible()
        {
            return !Body.CurrentRegion.IsNightTime;
        }
    }

    public class NightSpawnBrain : TimeDependentSpawnBrain
    {
        protected override bool ShouldBeVisible()
        {
            return Body.CurrentRegion.IsNightTime;
        }
    }
}
