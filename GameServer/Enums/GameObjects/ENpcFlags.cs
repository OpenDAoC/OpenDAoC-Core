using System;

namespace Core.GS;

/// <summary>
/// Various flags for this npc
/// </summary>
[Flags]
public enum ENpcFlags : uint
{
    /// <summary>
    /// The npc is translucent (like a ghost)
    /// </summary>
    GHOST = 0x01,
    /// <summary>
    /// The npc is stealthed (nearly invisible, like a stealthed player; new since 1.71)
    /// </summary>
    STEALTH = 0x02,
    /// <summary>
    /// The npc doesn't show a name above its head but can be targeted
    /// </summary>
    DONTSHOWNAME = 0x04,
    /// <summary>
    /// The npc doesn't show a name above its head and can't be targeted
    /// </summary>
    CANTTARGET = 0x08,
    /// <summary>
    /// Not in nearest enemyes if different vs player realm, but can be targeted if model support this
    /// </summary>
    PEACE = 0x10,
    /// <summary>
    /// The npc is flying (z above ground permitted)
    /// </summary>
    FLYING = 0x20,
    /// <summary>
    /// npc's torch is lit
    /// </summary>
    TORCH = 0x40,
    /// <summary>
    /// npc is a statue (no idle animation, no target...)
    /// </summary>
    STATUE = 0x80,
    /// <summary>
    /// npc is swimming
    /// </summary>
    SWIMMING = 0x100
}