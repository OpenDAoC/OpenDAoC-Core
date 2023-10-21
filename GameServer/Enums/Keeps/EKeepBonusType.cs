namespace Core.GS.Keeps;

/// <summary>
/// Bonus for owning certain amount of keeps
/// The int value is the amount of keeps needed
/// </summary>
public enum EKeepBonusType : int
{
    Coin_Drop_3 = 8,
    Experience_3 = 9,
    Bounty_Points_3 = 10,
    Craft_Timers_3 = 11,
    Coin_Drop_5 = 12,
    Experience_5 = 13,
    Bounty_Points_5 = 14,
    Craft_Timers_5 = 15,
    Power_Pool = 16,
    Endurance_Pool = 17,
    Power_Regeneration = 18,
    Health_Regeneration = 19,
    Melee_Critical = 20,
    Spell_Critical = 30
}