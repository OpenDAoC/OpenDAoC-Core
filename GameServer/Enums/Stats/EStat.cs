namespace Core.GS.Enums;

public enum EStat : byte
{
	UNDEFINED = 0,
	_First = EProperty.Stat_First,
	STR = EProperty.Strength,
	DEX = EProperty.Dexterity,
	CON = EProperty.Constitution,
	QUI = EProperty.Quickness,
	INT = EProperty.Intelligence,
	PIE = EProperty.Piety,
	EMP = EProperty.Empathy,
	CHR = EProperty.Charisma,
	ACU = EProperty.Acuity,
	_Last = EProperty.Stat_Last,
}