/// <summary>
/// Interface for attacker statistics
/// Used for attack calculations and damage determination
/// </summary>
public interface IAttackerStats
{
    int WeaponSkill { get; }
    int AttackSpeed { get; }
    int CriticalHitChance { get; }
    int ToHitBonus { get; }
}

/// <summary>
/// Interface for armor slot capabilities
/// Represents armor information for a specific equipment slot
/// </summary>
public interface IArmorSlotCapabilities
{
    bool HasArmor { get; }
    int ArmorFactor { get; }
    int ArmorAbsorption { get; }
    double Condition { get; }
}

/// <summary>
/// Interface for defense capabilities
/// Used to determine what defensive actions a character can perform
/// </summary>
public interface IDefenseCapabilities
{
    bool CanParry { get; }
    bool CanBlock { get; }
    bool CanEvade { get; }
}

/// <summary>
/// Interface for defense statistics
/// Used for defensive calculations
/// </summary>
public interface IDefenseStats
{
    int ArmorFactor { get; }
    int ArmorAbsorption { get; }
    int EvadeChance { get; }
    int ParryChance { get; }
    int BlockChance { get; }
} 