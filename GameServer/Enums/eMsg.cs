namespace DOL.GS;

/// <summary>
/// The type of message being sent, each of which has an associated eChatType and eChatLoc value associated. This enum value is used in the 'ChatUtil.SendTypeMessage()' call.
/// </summary>
public enum eMsg : int
{
    /// <summary>
    /// Use in instances where you aren't sure how to categorize
    /// </summary>
    None = 0,
    /// <summary>
    /// Use for the '/advice' channel or advisor messages
    /// </summary>
    Advice = 1,
    /// <summary>
    /// Use for alliance-based communication
    /// </summary>
    Alliance = 2,
    /// <summary>
    /// Use for staff announcements, such as with '/announce' subcommand
    /// </summary>
    Announce = 3,
    /// <summary>
    /// Use for Battlegroup communication
    /// </summary>
    Battlegroup = 4,
    /// <summary>
    /// Use for communication in a Battlegroup from the Leader
    /// </summary>
    BGLeader = 5,
    /// <summary>
    /// Use for the Broadcast channel
    /// </summary>
    Broadcast = 6,
    /// <summary>
    /// Use for messages to appear in the center of a player's screen as well as in the System window
    /// </summary>
    CenterSys = 7,
    /// <summary>
    /// Use for messages to appear in the Chat channel
    /// </summary>
    Chat = 8,
    /// <summary>
    /// Use for messages that display as a result of typing slash commands
    /// </summary>
    Command = 9,
    /// <summary>
    /// Use for messages that describe the purpose of a slash command
    /// </summary>
    CmdDesc = 10,
    /// <summary>
    /// Use for messages that preface a list of slash commands
    /// </summary>
    CmdHeader = 11,
    /// <summary>
    /// Use for messages that describe the syntax/structure of a slash command
    /// </summary>
    CmdSyntax = 12,
    /// <summary>
    /// Use for messages that describe the functionality of a slash command
    /// </summary>
    CmdUsage = 13,
    /// <summary>
    /// Use for messages that trigger due to damage add (DA) and damage shield (DS) spells
    /// </summary>
    DamageAddSh = 14,
    /// <summary>
    /// Use for messages that trigger when a player's target is damaged by them
    /// </summary>
    Damaged = 15,
    /// <summary>
    /// Use for admin/GM messages that should trigger to assist with troubleshooting
    /// </summary>
    Debug = 16,
    /// <summary>
    /// Use for messages that are triggered when a player performs an emote
    /// </summary>
    Emote = 17,
    /// <summary>
    /// Use for messages that other players see when a nearby player emotes
    /// </summary>
    EmoteSysOthers = 18,
    /// <summary>
    /// Use for messages that trigger when an error is encountered
    /// </summary>
    Error = 19,
    /// <summary>
    /// Use for spell expiration messages
    /// </summary>
    Expires = 20,
    /// <summary>
    /// Use for messages that trigger when a style failure occurs
    /// </summary>
    Failed = 21,
    /// <summary>
    /// Use for messages that appear in the Group/Party channel
    /// </summary>
    Group = 22,
    /// <summary>
    /// Use for messages that appear in the Guild channel
    /// </summary>
    Guild = 23,
    /// <summary>
    /// Use when help messages are triggered by a player
    /// </summary>
    Help = 24,
    /// <summary>
    /// Use when an important message is triggered
    /// </summary>
    Important = 25,
    /// <summary>
    /// Use for Albion kill spam
    /// </summary>
    KilledByAlb = 26,
    /// <summary>
    /// Use for Hibernia kill spam
    /// </summary>
    KilledByHib = 27,
    /// <summary>
    /// Use for Midgard kill spam
    /// </summary>
    KilledByMid = 28,
    /// <summary>
    /// Use for messages that appear in the LFG channel
    /// </summary>
    LFG = 29,
    /// <summary>
    /// Use for messages that display when loot is dropped
    /// </summary>
    Loot = 30,
    /// <summary>
    /// Use for messages that appear in the Merchant channel
    /// </summary>
    Merchant = 31,
    /// <summary>
    /// Use for general/generic messages that display
    /// </summary>
    Message = 32,
    /// <summary>
    /// Use for messages that convey an attack missed its target
    /// </summary>
    Missed = 33,
    /// <summary>
    /// Use for messages that appear in the Guild Officer channel
    /// </summary>
    Officer = 34,
    /// <summary>
    /// Use for messages that describe combat actions performed by other nearby players
    /// </summary>
    OthersCombat = 35,
    /// <summary>
    /// Use for messages that appear to a player when they die
    /// </summary>
    PlayerDied = 36,
    /// <summary>
    /// Use for messages that appear to other players when a nearby player dies (not including the originating client)
    /// </summary>
    PlayerDiedOthers = 37,
    /// <summary>
    /// Use for messages that trigger regularly as a result of a spell pulse
    /// </summary>
    Pulse = 38,
    /// <summary>
    /// Use for messages that trigger as a result of a spell being resisted
    /// </summary>
    Resisted = 39,
    /// <summary>
    /// Use for messages that trigger when a player speaks
    /// </summary>
    Say = 40,
    /// <summary>
    /// Use for messages that only trigger/appear in the center of a player's screen
    /// </summary>
    ScreenCenter = 41,
    /// <summary>
    /// Use for private messages sent between player clients
    /// </summary>
    Send = 42,
    /// <summary>
    /// Use for messages triggered as a result of tradeskills
    /// </summary>
    Skill = 43,
    /// <summary>
    /// Use for messages triggered as a result of a spell being cast
    /// </summary>
    Spell = 44,
    /// <summary>
    /// Use for messages that display from server admins/GMs
    /// </summary>
    Staff = 45,
    /// <summary>
    /// Use for messages that trigger when a combat style is successfully executed
    /// </summary>
    Success = 46,
    /// <summary>
    /// Use for System messages that display to all players near the triggering client (including the originating client)
    /// </summary>
    SysArea = 47,
    /// <summary>
    /// Use for System messages that display to all players near the triggering client, but not the originating client
    /// </summary>
    SysOthers = 48,
    /// <summary>
    /// Use for System messages
    /// </summary>
    System = 49,
    /// <summary>
    /// Use for messages that appear in the Team channel (only visible by admin/GM clients)
    /// </summary>
    Team = 50,
    /// <summary>
    /// Use for messages that appear in the Trade channel
    /// </summary>
    Trade = 51,
    /// <summary>
    /// Use for messages that appear when a player client "yells"
    /// </summary>
    Yell = 52,
    /// <summary>
    /// Use for messages that appear to a player client when they die
    /// </summary>
    YouDied = 53,
    /// <summary>
    /// Use for messages that appear when a player attacks and hits their target
    /// </summary>
    YouHit = 54,
    /// <summary>
    /// Use for messages that appear when a player is struck by an attacker
    /// </summary>
    YouWereHit = 55,
    /// <summary>
    /// Use for messages that appear when a player is struck by an attacker
    /// </summary>
    DialogWarn = 56,
    /// <summary>
    /// Use for messages that display to all players on a server
    /// </summary>
    Server = 56
}