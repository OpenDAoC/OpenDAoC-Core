namespace DOL.GS
{
    public class TurretFnfPet : TurretPet
    {
        public override double MaxHealthScalingFactor => 0.36;

        public TurretFnfPet(INpcTemplate template) : base(template) { }
    }
}
