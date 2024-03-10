namespace DOL.GS
{
    public interface IGameEpicNpc
    {
        public double DefaultArmorFactorScalingFactor { get; }
        public int ArmorFactorScalingFactorPetCap { get; }
        public double ArmorFactorScalingFactor { get; set; }
    }
}
