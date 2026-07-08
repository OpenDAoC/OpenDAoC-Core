namespace DOL.GS
{
    public enum RandomEvent : byte
    {
        Intercept,                // Primarily used by Spiritmaster pets.
        Evade,
        Parry,
        Block,                    // Includes Guard.
        Miss,
        Resist,
        DualWield,                // Off-hand attacks for CD/DW/H2H.
        OffensiveProcChance,      // Weapon and spell based offensive procs.
        DefensiveProcChance,      // Armor and spell based defensive procs.
        PhysicalVariance,
        MagicVariance,
        PhysicalCriticalChance,
        MagicCriticalChance,
        PhysicalCriticalVariance,
        MagicCriticalVariance
    }
}
