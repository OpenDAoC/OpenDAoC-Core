namespace DOL.GS;

/// <summary>
/// The possible ambient triggers for GameNPC actions (e.g., killing, roaming, dying)
/// </summary>
public enum EAmbientNpcTrigger
{
    spawning,
    dying,
    aggroing,
    fighting,
    roaming,
    killing,
    moving,
    interact,
    seeing
}