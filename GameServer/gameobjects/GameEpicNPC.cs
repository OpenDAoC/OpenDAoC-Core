namespace DOL.GS
{
    public class GameEpicNPC : GameNPC, IGameEpicNpc
    {
        public override double MaxHealthScalingFactor => 1.25 * RaidEncounterHealthScalingFactor;
        public double DefaultArmorFactorScalingFactor => 0.8;
        public int ArmorFactorScalingFactorPetCap => 16;
        public double ArmorFactorScalingFactor => EpicNpcArmorFactor.Resolve(this, this);

        public GameEpicNPC() : base()
        {
            DamageFactor = 1.5;
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

        public override short MaxSpeedBase => (short) (191 + Level * 2);

        public override int MaxHealth => 10000 + Level * 125;
    }
}
