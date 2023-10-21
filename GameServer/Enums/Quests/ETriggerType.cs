namespace DOL.GS.Behaviour;

/// <summary>
/// Triggertype defines a list of available triggers to use within questparts.
/// Depending on triggertype triggerVariable and triggerkeyword will have special
/// meaning look at documentation of each triggertype for details
/// </summary>
///<remarks>
/// Syntax: ... I:eEmote(eEmote.Yes) ... Parameter I must be of Type
/// eEmote and has the default value of "eEmote.Yes" (used if no value is passed).
/// If no default value is defined value must be passed along with trigger.
/// </remarks>
public enum ETriggerType : byte
{
    /// <summary>
    /// No Triggertype at all, needed since triggertype cannot be null when passed as an argument
    /// </summary>
    None = 0x00,
    //ATTA : Monster acquires a target and initiate an attack                 
    /// <summary>
    /// AQST : player accepts quest I:Type[Typename:string](Current Quest)
    /// </summary>       
    /// <remarks>Tested</remarks>
    AcceptQuest = 0x01,
    /// <summary>
    /// DEAT : Enemy I:GameLiving[GameLiving's Id or Name:string](NPC) died, no matter who/what killed it
    /// </summary>
    EnemyDying = 0x02,
    /// <summary>
    /// DQST : Player declines quest I:Type[Typename:string](Current Quest)
    /// </summary>
    /// <remarks>Tested</remarks>
    DeclineQuest = 0x03,
    /// <summary>
    /// Continue quest I:Type[Typename:string](Current Quest) after abort quest offer
    /// </summary>
    /// <remarks>Tested</remarks>
    ContinueQuest = 0x04,
    /// <summary>
    /// Abort quest I:Type[Typename:string](Current Quest) after abort quest offer
    /// </summary>
    /// <remarks>Tested</remarks>
    AbortQuest = 0x05,
    //INRA : player enters interaction radius (checked each second, please use timers)
    /// <summary>
    /// player enters area I:IArea        
    /// </summary>
    /// <remarks>Tested</remarks>
    EnterArea = 0x06,
    /// <summary>
    /// player leaves area I:IArea
    /// </summary>
    /// <remarks>Tested</remarks>
    LeaveArea = 0x07,
    /// <summary>
    /// INTE : player right-clicks I:GameLiving[GameLiving's Id or Name:string](NPC)
    /// </summary>
    /// <remarks>Tested</remarks>
    Interact = 0x08,
    /// <summary>
    ///  KILL : Monster I:GameLiving[GameLiving's Id or Name:string](NPC) kills the quest player
    /// </summary>
    PlayerKilled = 0x09,
    /// <summary>
    /// PKIL : a player (and only a player) kills any monster with the name K:string or a specific monster I:GameLiving[GameLiving's Id or Name:string](NPC)
    /// if TriggerKeyword != null
    ///     triggerkeyword = name of monster
    /// else
    ///     I = monster object
    /// </summary>        
    EnemyKilled = 0x0A,
    /// <summary>
    /// TAKE : Player gives item I:ItemTemplate[Item's Id_nb:string] to K:GameLiving[GameLiving's Id or Name:string](NPC)
    /// </summary>
    /// <remarks>Tested</remarks>
    GiveItem = 0x0B,
    /// <summary>
    /// WORD : player whispers the word K:string to I:GameLiving[GameLiving's Id or Name:string](NPC)
    /// </summary>
    /// <remarks>Tested</remarks>
    Whisper = 0x0C,
    //TIME : ingame time reaches I
    /// <summary>
    /// TMR# :  timer with id K:string has finished
    /// </summary>
    Timer = 0x0D,
    /*
    Description of item specific trigger types :
    BLOC : item blocks an attack
    BOUG : item is bought
    DROP : item is droped
    EREG : item's holder enters a region
    EVAD : item's holder evades an attack
    FUMB : item fumbles an attack
    GET : item is picked up
    HIT : item's holder is hit
    LREG : item's holder leaves a region
    MISS : item's holder is missed
    PARR : item parries an attack
    REMO : item is removed
    SHEA : item is sheathed
    SOLD : item is sold
    SPEL : item's holder casts a spell
    STRI : item hits a monster
    STYL : item's holder performs a style
     * */
    /// <summary>
    /// USED : Item I:ItemTemplate[Item's Id_nb:string] is used by player
    /// </summary>
    /// <remarks>Tested</remarks>
    ItemUsed = 0x0F
    /*
        WIEL : item is wield
        WORN : item is worn
        */
}