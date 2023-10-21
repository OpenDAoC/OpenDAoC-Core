namespace Core.GS.Enums;

/// <summary>
/// Requirementtype defines a list of requirements to be used with questparts.
/// Depending on requirmenttype V and N will have special
/// meaning look at documentation of each requirementtype for details    
/// </summary>
///<remarks>
/// Syntax: ... V:eEmote(eEmote.Yes) ... Parameter V must be of Type
/// eEmote and has the default value of "eEmote.Yes" (used if no value is passed).
/// If no default value is defined value must be passed along with action.
/// </remarks>
public enum ERequirementType : byte
{
    /// <summary>
    /// No Requirement
    /// </summary>
    /// <remarks>Tested</remarks>
    None,
    /// <summary>
    /// AINV : checks inventory for V:int(1)[string] instances of item N:ItemTemplate[Item's Id_nb:string]
    /// </summary>
    /// <remarks>Tested</remarks>
    InventoryItem,
    // AREQ : checks requirement list r##n# (requirement lists described further)
    /// <summary>
    /// CLAS : checks for player class N:int[string]
    /// </summary>
    Class,
    // /// <summary>
    // /// COMB : checks for player combat skill N at level V:int[string]
    // /// </summary>
    //CombatSkill = 0x04,
    /// <summary>
    /// CQST : checks for quest N:Type[Typename:string](Current Quest) completed V:int[string] times
    /// </summary>
    Quest,
    /// <summary>
    /// ENCU : compares player's encumbrance value with N:int[string]
    /// </summary>
    Encumbrance,
    /// <summary>
    /// MENC : compares player's maximum encumbrance value with N:int[string]
    /// </summary>
    EncumbranceMax,
    // // <summary>
    // /// FACT : checks for faction N at level N
    // /// </summary>
    //Faction=0x07,	
    /// <summary>
    /// FATG : compares player's fatigue value with N:int[string] using eComparator
    /// </summary>
    Endurance,
    /// <summary>
    /// MFAT : compares player's maximum fatigue value with N:int[string] using eComparator
    /// </summary>
    EnduranceMax,
    // // <summary>
    // /// GLEV : compares guild level with N:int[string] (not player's guilds) using eComparator
    // /// </summary>
    //GuildLevel=0x09,
    /// <summary>
    /// GNDR : compares player's gender with N:int[string] {0=male,1=female}
    /// </summary>
    Gender,
    /// <summary>
    /// GRUP : checks if player is grouped. N:int[string] is number of people in group
    /// </summary>
    GroupNumber,
    /// <summary>
    /// GPLV : checks if player is grouped. N:int[string] is sum of levels in group
    /// </summary>
    GroupLevel,
    /// <summary>
    /// GUIL : compares guildname of N:GameLiving(NPC)[NPC's ID:string] with V:string.
    /// </summary>
    Guild,
    /// <summary>
    /// Compares players gold with N:long[string] using eComparator
    /// </summary>
    Gold,
    /// <summary>
    /// HITS : compares player's current hit points with N:int[string] using eComparator
    /// </summary>
    Health,
    /// <summary>
    /// HITS : compares player's maximum hit points with N:int[string] using eComparator
    /// </summary>
    HealthMax,
    /// <summary>
    /// LEVE : compares player's level with N:int[string] using eComparator
    /// </summary>
    Level,
    /// <summary>
    /// POWR: compares player's current mana value with N:int[string] using eComparator
    /// </summary>
    Mana,
    /// <summary>
    /// MPOW: compares player's maximum mana value with N:int[string] using eComparator
    /// </summary>
    ManaMax,
    /// <summary>
    /// PQST : checks for player's pending quest N:Type[Typename:string](Current Quest)
    /// </summary>
    /// <remarks>Tested</remarks>
    QuestPending,
    /// <summary>
    /// RACE : compares player's race with N:int[string]
    /// </summary>
    Race,
    /// <summary>
    /// RAND : percent random chance indicated by N:int[string]
    /// </summary>
    Random,
    /// <summary>
    /// REAL : compares player's realm with N:int[string]
    /// </summary>
    Realm,
    /// <summary>
    /// RLEV : compares player's realm level with N:int[string] using eComparator
    /// </summary>
    RealmLevel,
    /// <summary>
    /// RPTS : compares player's realm points with N:long[string] using eComparator
    /// </summary>
    RealmPoints,
    /// <summary>
    /// REGO : compare player's zone with N:int[string] and region with V:int[string]
    /// </summary>
    Region,
    /// <summary>
    /// RINV : checks readied/worn items of player for item N:ItemTemplate[Item's Id_nb:string]
    /// </summary>
    EquippedItem,
    /*
    SHOU : checks for shout r##n# at value r##v#
    SKIL : checks for skill r##n# at level r##v#
    SONG : checks for song r##n# at value r##v#
    SPEL : checks for spell r##n# at value r##v#
    STAT : checks for stat r##n# at value r##v#
    STYL : checks for style r##n# at level r##v#
    */
    //TIMR : checks when timer indicated by r##n# meet time r##v# (player side)
    /// <summary>
    /// QUES : checks for player's quest N:Type[Typename:string](Current Quest) at step V:int[string] using eComparator
    /// </summary>
    /// <remarks>Tested</remarks>
    QuestStep,
    /// <summary>
    /// Checks for quest N:Type[Typename:string](Current Quest) to be givable by NPC to player
    /// </summary>
    /// <remarks>Tested</remarks>
    QuestGivable,
    /// <summary>
    /// Compares distance V:int[string] between player and given GameObject N:GameLiving[GameLiving's Name:string](NPC) using Comparator
    /// </summary>
    /// <remarks>Tested</remarks>
    Distance
}