using Core.GS.Enums;

namespace Core.GS;

public abstract class GameDoorBase : GameLiving
{
    public override EGameObjectType GameObjectType => EGameObjectType.DOOR;

    public abstract uint Flag { get; set; } // Used to identify what sound a door makes when open / close.
    public abstract int DoorID { get; set; }
    public abstract EDoorState State { get; set; }

    public abstract void Close(GameLiving closer = null);
    public abstract void NPCManipulateDoorRequest(GameNpc npc, bool open);
    public abstract void Open(GameLiving opener = null);
}