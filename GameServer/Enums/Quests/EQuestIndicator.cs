namespace Core.GS.Enums;

public enum EQuestIndicator : byte
{
	None = 0x00,
	Available = 0x01,
	Finish = 0x02,
	Lesson = 0x04,
	Lore = 0x08,
	Pending = 0x10, // patch 0031
}