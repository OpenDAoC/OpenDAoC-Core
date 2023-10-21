using System;

namespace DOL.GS.Housing;

[Flags]
public enum EVaultPermissions : byte
{
    None = 0x00,
    Add = 0x01,
    Remove = 0x02,
    View = 0x04
}