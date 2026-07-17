namespace OpenDAoC.Pathing
{
    [Flags]
    public enum EDtPolyFlags : ushort
    {
        Walk = 0x01,
        Swim = 0x02,
        Door = 0x04,
        Jump = 0x08,
        Disabled = 0x10,
        All = 0xffff,

        // Runtime flags (not stored in the navmesh; used by game logic).
        BlockingDoor = 0x20,
        AnyDoor = Door | BlockingDoor
    }

    [Flags]
    public enum EDtStatus : uint
    {
        DT_FAILURE = 1u << 31,
        DT_SUCCESS = 1u << 30,
        DT_IN_PROGRESS = 1u << 29,

        DT_STATUS_DETAIL_MASK = 0x0FFFFFFF,
        DT_WRONG_MAGIC = 1 << 0,
        DT_WRONG_VERSION = 1 << 1,
        DT_OUT_OF_MEMORY = 1 << 2,
        DT_INVALID_PARAM = 1 << 3,
        DT_BUFFER_TOO_SMALL = 1 << 4,
        DT_OUT_OF_NODES = 1 << 5,
        DT_PARTIAL_RESULT = 1 << 6,
        DT_ALREADY_OCCUPIED = 1 << 7
    }

    public enum EDtStraightPathOptions : uint
    {
        DT_STRAIGHTPATH_NO_CROSSINGS = 0x00,
        DT_STRAIGHTPATH_AREA_CROSSINGS = 0x01,
        DT_STRAIGHTPATH_ALL_CROSSINGS = 0x02,
    }

    public static class DetourStatusExtensions
    {
        public static bool Succeeded(this EDtStatus status)
        {
            return (status & EDtStatus.DT_SUCCESS) != 0;
        }

        public static bool Failed(this EDtStatus status)
        {
            return (status & EDtStatus.DT_FAILURE) != 0;
        }

        public static bool IsPartial(this EDtStatus status)
        {
            return (status & EDtStatus.DT_PARTIAL_RESULT) != 0;
        }
    }
}
