namespace Core.GS.Enums;

/// <summary>
/// Used for that formation type if a GameNPC has a formation
/// </summary>
public enum EPetFormationType
{
    // M = owner
    // x = following npcs
    //Line formation
    // M x x x
    Line,
    //Triangle formation
    //		x
    // M x
    //		x
    Triangle,
    //Protect formation
    //		 x
    // x  M
    //		 x
    Protect,
}