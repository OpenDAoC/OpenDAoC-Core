using System;

namespace Core.GS.Housing;

[Flags]
public enum EDecorationPermissions : byte
{
    None = 0x00,
    Add = 0x01,
    Remove = 0x02
}