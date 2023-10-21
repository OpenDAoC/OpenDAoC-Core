namespace Core.GS.Behaviour;

/// <summary>
/// Comparator enume used within some of the requirement checks.
/// </summary>
public enum EComparator : byte
{
    /// <summary>
    /// No check is done, will always return true
    /// </summary>
    None = 0,

    /// <summary>
    /// Checks wether given value1 is less than value2
    /// </summary>
    Less = 1,

    /// <summary>
    /// Checks wether given value1 is greater than value2
    /// </summary>
    Greater = 2,

    /// <summary>
    ///  Checks wether given value1 is equal value2
    /// </summary>
    Equal = 3,

    /// <summary>
    /// Checks wether given value1 is not equal value2
    /// </summary>
    NotEqual = 4,

    /// <summary>
    /// Negotiation of given argument
    /// usable with QuestPending, QuestGivable
    /// </summary>
    Not = 5
}