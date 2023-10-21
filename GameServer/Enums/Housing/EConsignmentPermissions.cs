using System;

namespace DOL.GS.Housing;

[Flags]
public enum EConsignmentPermissions : byte
{
    AddRemove = 0x03,
    Withdraw = 0x10,
    Any = AddRemove | Withdraw
}