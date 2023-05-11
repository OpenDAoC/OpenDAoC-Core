namespace DOL.GS;

/// <summary>
/// The type of dialog window to display. This enum value is used in the 'ChatUtil.SendWindowMessage()' method.
/// </summary>
public enum eWindow : int
{
    /// <summary>
    /// Use for dialogs that display only text.
    /// </summary>
    Text = 1,
    /// <summary>
    /// Use for timer windows.
    /// </summary>
    Timer = 2
}