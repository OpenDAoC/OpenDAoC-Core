namespace DOL.GS
{
    public struct StatsComponent
    {
        int Strength;
        int Dexterity;
        int Constitution;
        int Quickness;
        int Intelligence;
        int Piety;
        int Empathy;
        int Charisma;

        public int GetStat(eStat stat)
        {
            switch (stat)
            {
                case eStat.STR:
                    return Strength;
                case eStat.DEX:
                    return Dexterity;
                case eStat.CON:
                    return Constitution;
                case eStat.QUI:
                    return Quickness;
                case eStat.INT:
                    return Intelligence;
                case eStat.PIE:
                    return Piety;
                case eStat.EMP:
                    return Empathy;
                case eStat.CHR:
                    return Charisma;
                default:
                    return 0;
            }
        }

        public void SetStat(eStat stat, int value)
        {
            switch (stat)
            {
                case eStat.STR:
                    this.Strength = value;
                    break;
                case eStat.DEX:
                    this.Dexterity = value;
                    break;
                case eStat.CON:
                    this.Constitution = value;
                    break;
                case eStat.QUI:
                    this.Quickness = value;
                    break;
                case eStat.INT:
                    this.Intelligence = value;
                    break;
                case eStat.PIE:
                    this.Piety = value;
                    break;
                case eStat.EMP:
                    this.Empathy = value;
                    break;
                case eStat.CHR:
                    this.Charisma = value;
                    break;
                default:
                    break;
            }
        }

        public int IncreaseStat(eStat stat, int valueToIncreaseBy)
        {
            switch (stat)
            {
                case eStat.STR:
                    Strength += valueToIncreaseBy;
                    return Strength;
                case eStat.DEX:
                    Dexterity += valueToIncreaseBy;
                    return Dexterity;
                case eStat.CON:
                    Constitution += valueToIncreaseBy;
                    return Constitution;
                case eStat.QUI:
                    Quickness += valueToIncreaseBy;
                    return Quickness;
                case eStat.INT:
                    Intelligence += valueToIncreaseBy;
                    return Intelligence;
                case eStat.PIE:
                    Piety += valueToIncreaseBy;
                    return Piety;
                case eStat.EMP:
                    Empathy += valueToIncreaseBy;
                    return Empathy;
                case eStat.CHR:
                    Charisma += valueToIncreaseBy;
                    return Charisma;
                default:
                    return 0;
            }
        }

        public int DecreaseStat(eStat stat, int valueToDecreaseBy)
        {
            switch (stat)
            {
                case eStat.STR:
                    Strength -= valueToDecreaseBy;
                    return Strength;
                case eStat.DEX:
                    Dexterity -= valueToDecreaseBy;
                    return Dexterity;
                case eStat.CON:
                    Constitution -= valueToDecreaseBy;
                    return Constitution;
                case eStat.QUI:
                    Quickness -= valueToDecreaseBy;
                    return Quickness;
                case eStat.INT:
                    Intelligence -= valueToDecreaseBy;
                    return Intelligence;
                case eStat.PIE:
                    Piety -= valueToDecreaseBy;
                    return Piety;
                case eStat.EMP:
                    Empathy -= valueToDecreaseBy;
                    return Empathy;
                case eStat.CHR:
                    Charisma -= valueToDecreaseBy;
                    return Charisma;
                default:
                    return 0;
            }
        }
    }

}
