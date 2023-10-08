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

        public int GetStat(EStat stat)
        {
            switch (stat)
            {
                case EStat.STR:
                    return Strength;
                case EStat.DEX:
                    return Dexterity;
                case EStat.CON:
                    return Constitution;
                case EStat.QUI:
                    return Quickness;
                case EStat.INT:
                    return Intelligence;
                case EStat.PIE:
                    return Piety;
                case EStat.EMP:
                    return Empathy;
                case EStat.CHR:
                    return Charisma;
                default:
                    return 0;
            }
        }

        public void SetStat(EStat stat, int value)
        {
            switch (stat)
            {
                case EStat.STR:
                    this.Strength = value;
                    break;
                case EStat.DEX:
                    this.Dexterity = value;
                    break;
                case EStat.CON:
                    this.Constitution = value;
                    break;
                case EStat.QUI:
                    this.Quickness = value;
                    break;
                case EStat.INT:
                    this.Intelligence = value;
                    break;
                case EStat.PIE:
                    this.Piety = value;
                    break;
                case EStat.EMP:
                    this.Empathy = value;
                    break;
                case EStat.CHR:
                    this.Charisma = value;
                    break;
                default:
                    break;
            }
        }

        public int IncreaseStat(EStat stat, int valueToIncreaseBy)
        {
            switch (stat)
            {
                case EStat.STR:
                    Strength += valueToIncreaseBy;
                    return Strength;
                case EStat.DEX:
                    Dexterity += valueToIncreaseBy;
                    return Dexterity;
                case EStat.CON:
                    Constitution += valueToIncreaseBy;
                    return Constitution;
                case EStat.QUI:
                    Quickness += valueToIncreaseBy;
                    return Quickness;
                case EStat.INT:
                    Intelligence += valueToIncreaseBy;
                    return Intelligence;
                case EStat.PIE:
                    Piety += valueToIncreaseBy;
                    return Piety;
                case EStat.EMP:
                    Empathy += valueToIncreaseBy;
                    return Empathy;
                case EStat.CHR:
                    Charisma += valueToIncreaseBy;
                    return Charisma;
                default:
                    return 0;
            }
        }

        public int DecreaseStat(EStat stat, int valueToDecreaseBy)
        {
            switch (stat)
            {
                case EStat.STR:
                    Strength -= valueToDecreaseBy;
                    return Strength;
                case EStat.DEX:
                    Dexterity -= valueToDecreaseBy;
                    return Dexterity;
                case EStat.CON:
                    Constitution -= valueToDecreaseBy;
                    return Constitution;
                case EStat.QUI:
                    Quickness -= valueToDecreaseBy;
                    return Quickness;
                case EStat.INT:
                    Intelligence -= valueToDecreaseBy;
                    return Intelligence;
                case EStat.PIE:
                    Piety -= valueToDecreaseBy;
                    return Piety;
                case EStat.EMP:
                    Empathy -= valueToDecreaseBy;
                    return Empathy;
                case EStat.CHR:
                    Charisma -= valueToDecreaseBy;
                    return Charisma;
                default:
                    return 0;
            }
        }
    }

}
