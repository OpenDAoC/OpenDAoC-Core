namespace DOL.GS
{
    public class TurretMainPetCaster : TurretPet
    {
        public override double MaxHealthScalingFactor => 0.8;

        public TurretMainPetCaster(INpcTemplate template) : base(template) { }
    }
}
