namespace DOL.GS
{
    public enum eDoorState
    {
        Open,
        Closed
    }

    public abstract class GameDoorBase : GameLiving
    {
        public abstract uint Flag { get; }
        public abstract ushort ZoneID { get; }
        public abstract int DoorID { get; set; }
        public abstract eDoorState State { get; set; }

        public abstract void Close(GameLiving closer = null);
        public abstract void NPCManipulateDoorRequest(GameNPC npc, bool open);
        public abstract void Open(GameLiving opener = null);
    }
}
