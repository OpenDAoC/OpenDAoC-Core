namespace DOL.GS
{
    public enum eDoorState
    {
        Open,
        Closed
    }

    public abstract class GameDoorBase : GameLiving
    {
        public override eGameObjectType GameObjectType => eGameObjectType.DOOR;

        public abstract uint Flag { get; set; } // Used to identify what sound a door makes when open / close.
        public abstract int DoorID { get; set; }
        public abstract eDoorState State { get; set; }

        public abstract void Close(GameLiving closer = null);
        public abstract void NPCManipulateDoorRequest(GameNPC npc, bool open);
        public abstract void Open(GameLiving opener = null);
    }
}
