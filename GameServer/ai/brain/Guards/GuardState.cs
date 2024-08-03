namespace DOL.AI.Brain
{
    public class GuardState_RETURN_TO_SPAWN : StandardMobState_RETURN_TO_SPAWN
    {
        protected override short Speed => _brain.Body.MaxSpeed;

        public GuardState_RETURN_TO_SPAWN(StandardMobBrain brain) : base(brain) { }
    }
}
