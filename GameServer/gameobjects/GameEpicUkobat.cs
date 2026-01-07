namespace DOL.GS
{
    public class GameEpicUkobat : GameNPC, IGameEpicNpc
    {
        public override double MaxHealthScalingFactor => 1.25;
        public double DefaultArmorFactorScalingFactor => 0.8;
        public int ArmorFactorScalingFactorPetCap => 16;
        public double ArmorFactorScalingFactor { get; set; }

        public GameEpicUkobat() : base()
        {
            DamageFactor = 1.5;
            ArmorFactorScalingFactor = DefaultArmorFactorScalingFactor;
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive)
            {
                if (keyName is GS.Abilities.ConfusionImmunity or GS.Abilities.NSImmunity)
                    return true;
            }

            return base.HasAbility(keyName);
        }

        public override int MaxHealth => 10000 + Level * 125;
    }
}
