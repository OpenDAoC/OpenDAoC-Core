namespace DOL.GS
{
    /// <summary>
    /// Session-only powers enabled for a GM player.
    /// </summary>
    public abstract class GMPowers
    {
        /// <summary>
        /// Shared null object used by players who have no powers enabled.
        /// </summary>
        public static GMPowers None => GMPowersFacade.Instance;

        public abstract bool GodModeEnabled { get; set; }
        public abstract bool AttackableEnabled { get; set; }
        public abstract bool DamageBoostEnabled { get; set; }
        public abstract double DamageMultiplier { get; set; }

        internal void EnableDamageBoost(double multiplier)
        {
            DamageBoostEnabled = true;
            DamageMultiplier = multiplier;
        }

        internal void DisableDamageBoost()
        {
            DamageBoostEnabled = false;
            DamageMultiplier = 1.0;
        }

        internal static GMPowers GetOrCreate(GamePlayer player)
        {
            GMPowers powers = player.GMPowers;

            if (ReferenceEquals(powers, None))
            {
                powers = new PlayerGMPowers();
                player.GMPowers = powers;
            }

            return powers;
        }

        internal static void RemoveIfInactive(GamePlayer player)
        {
            if (!player.GMPowers.AnyEnabled)
                player.GMPowers = None;
        }

        internal static void DisableAll(GamePlayer player)
        {
            GMPowers powers = player.GMPowers;
            powers.GodModeEnabled = false;
            powers.AttackableEnabled = false;
            powers.DisableDamageBoost();
            player.GMPowers = None;
        }

        private bool AnyEnabled => GodModeEnabled || AttackableEnabled || DamageBoostEnabled;
    }

    /// <summary>
    /// Null-object facade returned when a player has no GM powers enabled.
    /// </summary>
    internal sealed class GMPowersFacade : GMPowers
    {
        internal static GMPowersFacade Instance { get; } = new();

        private GMPowersFacade()
        {
        }

        public override bool GodModeEnabled { get => false; set { } }
        public override bool AttackableEnabled { get => false; set { } }
        public override bool DamageBoostEnabled { get => false; set { } }
        public override double DamageMultiplier { get => 1.0; set { } }
    }

    /// <summary>
    /// Mutable power state belonging to one player.
    /// </summary>
    internal sealed class PlayerGMPowers : GMPowers
    {
        private volatile bool _godModeEnabled;
        private volatile bool _attackableEnabled;
        private volatile bool _damageBoostEnabled;
        private double _damageMultiplier = 1.0;

        public override bool GodModeEnabled
        {
            get => _godModeEnabled;
            set => _godModeEnabled = value;
        }

        public override bool AttackableEnabled
        {
            get => _attackableEnabled;
            set => _attackableEnabled = value;
        }

        public override bool DamageBoostEnabled
        {
            get => _damageBoostEnabled;
            set => _damageBoostEnabled = value;
        }

        public override double DamageMultiplier
        {
            get => _damageMultiplier;
            set => _damageMultiplier = value;
        }
    }
}
