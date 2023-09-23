namespace DOL.Database;

/// <summary>
/// Skin types to use for this keep
/// 0 = any, 1 = old, 2 = new
/// </summary>
public enum EKeepSkinType : byte
{
    /// <summary>
    /// Use server proerty to determine skin
    /// </summary>
    Any = 0,
    /// <summary>
    /// Use old skins
    /// </summary>
    Old = 1,
    /// <summary>
    /// Use new skins
    /// </summary>
    New = 2,
}