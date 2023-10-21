namespace Core.GS.Enums;

/// <summary>
/// Type of textoutput this one is used for general text messages within questpart.   
/// </summary>
public enum ETextType : byte
{
    /// <summary>
    /// No output at all
    /// </summary>
    None = 0x00,
    /// <summary>
    /// EMOT : display the text localy without monster's name (local channel)
    /// </summary>
    /// <remarks>Tested</remarks>
    Emote = 0x01,
    /// <summary>
    /// BROA : broadcast the text in the entire zone (broadcast channel)
    /// </summary>
    Broadcast = 0x02,
    /// <summary>
    /// DIAG : display the text in a dialog box with an OK button
    /// </summary>
    Dialog = 0x03,
    /// <summary>
    /// READ : open a description (bracket) windows saying what is written on the item
    /// </summary>
    Read = 0x05,
    /// <summary>
    /// NPC says something
    /// </summary>
    Say = 0x06,
    /// <summary>
    /// NPC yells something
    /// </summary>
    Yell = 0x07,
    /// <summary>
    /// NPC says something to target
    /// </summary>
    SayTo = 0x08,
}