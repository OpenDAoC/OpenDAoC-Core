using System;

namespace Core.GS;

/// <summary>
/// All property types for check using SkillBase.CheckPropertyType. Must be unique bits set.
/// </summary>
[Flags]
public enum EPropertyType : ushort
{
	Focus = 1,
	Resist = 1 << 1,
	Skill = 1 << 2,
	SkillMeleeWeapon = 1 << 3,
	SkillMagical = 1 << 4,
	SkillDualWield = 1 << 5,
	SkillArchery = 1 << 6,
	ResistMagical = 1 << 7,
	Albion = 1 << 8,
	Midgard = 1 << 9,
	Hibernia = 1 << 10,
	Common = 1 << 11,
	CapIncrease = 1 << 12,
}