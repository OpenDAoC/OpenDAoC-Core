namespace DOL.GS;

public enum ERangedAttackType : byte
{
    /// <summary>
    /// A normal ranged attack
    /// </summary>
    Normal = 0,
    /// <summary>
    /// A critical shot is attempted
    /// </summary>
    Critical,
    /// <summary>
    /// A longshot is attempted
    /// </summary>
    Long,
    /// <summary>
    /// A volley shot is attempted
    /// </summary>
    Volley,
    /// <summary>
    /// A sure shot is attempted
    /// </summary>
    SureShot,
    /// <summary>
    /// A rapid shot is attempted
    /// </summary>
    RapidFire,
}