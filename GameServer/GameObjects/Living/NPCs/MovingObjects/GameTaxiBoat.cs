using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS;

/*
 * These boats are very fast, and can carry up to sixteen passengers.
 * You have thirty seconds to board the boat before it sets sail.
 * You can board the boat by double clicking on it,
 * typing /vboard or using your `Get key' with the boat targeted.
 * You will automatically leave the boat when it reaches its destination,
 * but if you wish to leave before then, just type `/disembark'.
 * or press the jump key
 */
public class GameTaxiBoat : GameMovingObject
{
    public GameTaxiBoat() : base()
    {
        Model = 2650;
        Level = 0;
        Flags = ENpcFlags.PEACE;
        Name = "boat";
        MaxSpeedBase = 1000;
        BlankBrain brain = new();
        SetOwnBrain(brain);
    }

    public override int InteractDistance => 666;

    public override int MAX_PASSENGERS => 16;

    public override int SLOT_OFFSET => 1;

    public override short MaxSpeed => 1000;
}