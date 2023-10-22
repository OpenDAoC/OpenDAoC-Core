using System;

namespace Core.GS.Enums;

/// <summary>
/// The client addons enum
/// </summary>
[Flags]
public enum EClientAddons
{
    bit4 = 0x10,
    NewNewFrontiers = 0x20,
    Foundations = 0x40,
    NewFrontiers = 0x80,
}