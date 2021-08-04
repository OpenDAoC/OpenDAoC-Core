namespace DOL.GS
{
    public struct ResistsComponent
    {
        int Body;
        int Cold;
        int Crush;
        int Energy;
        int Heat;
        int Matter;
        int Natural;
        int Slash;
        int Spirit;
        int Thrust;


        /// <summary>
        /// Cap for player cast resist buffs.
        /// </summary>
        public static int BuffBonusCap {
            get { return 24; }
        }

        /// <summary>
        /// Hard cap for resists.
        /// </summary>
        public static int HardCap {
            get { return 70; }
        }

        public int GetResist(eResist resist)
        {
            switch (resist)
            {
                case eResist.Body:
                    return Body;
                case eResist.Cold:
                    return Cold;
                case eResist.Crush:
                    return Crush;
                case eResist.Energy:
                    return Energy;
                case eResist.Heat:
                    return Heat;
                case eResist.Matter:
                    return Matter;
                case eResist.Natural:
                    return Natural;
                case eResist.Slash:
                    return Slash;
                case eResist.Spirit:
                    return Spirit;
                case eResist.Thrust:
                    return Thrust;
                default:
                    return 0;
            }
        }

        public void SetResist(eResist resist, int value)
        {
            switch (resist)
            {
                case eResist.Body:
                    this.Body = value;
                    break;
                case eResist.Cold:
                    this.Cold = value;
                    break;
                case eResist.Crush:
                    this.Crush = value;
                    break;
                case eResist.Energy:
                    this.Energy = value;
                    break;
                case eResist.Heat:
                    this.Heat = value;
                    break;
                case eResist.Matter:
                    this.Matter = value;
                    break;
                case eResist.Natural:
                    this.Natural = value;
                    break;
                case eResist.Slash:
                    this.Slash = value;
                    break;
                case eResist.Spirit:
                    this.Spirit = value;
                    break;
                case eResist.Thrust:
                    this.Thrust = value;
                    break;
                default:
                    break;
            }
        }

        public int IncreaseResist(eResist resist, int valueToIncreaseBy)
        {
            switch (resist)
            {
                case eResist.Body:
                    Body += valueToIncreaseBy;
                    return Body;
                case eResist.Cold:
                    Cold += valueToIncreaseBy;
                    return Cold;
                case eResist.Crush:
                    Crush += valueToIncreaseBy;
                    return Crush;
                case eResist.Energy:
                    Energy += valueToIncreaseBy;
                    return Energy;
                case eResist.Heat:
                    Heat += valueToIncreaseBy;
                    return Heat;
                case eResist.Matter:
                    Matter += valueToIncreaseBy;
                    return Matter;
                case eResist.Natural:
                    Natural += valueToIncreaseBy;
                    return Natural;
                case eResist.Slash:
                    Slash += valueToIncreaseBy;
                    return Slash;
                case eResist.Spirit:
                    Spirit += valueToIncreaseBy;
                    return Spirit;
                case eResist.Thrust:
                    Thrust += valueToIncreaseBy;
                    return Thrust;
                default:
                    return 0;
            }
        }

        public int DecreaseStat(eResist resist, int valueToDecreaseBy)
        {
            switch (resist)
            {
                case eResist.Body:
                    Body += valueToDecreaseBy;
                    return Body;
                case eResist.Cold:
                    Cold += valueToDecreaseBy;
                    return Cold;
                case eResist.Crush:
                    Crush += valueToDecreaseBy;
                    return Crush;
                case eResist.Energy:
                    Energy += valueToDecreaseBy;
                    return Energy;
                case eResist.Heat:
                    Heat += valueToDecreaseBy;
                    return Heat;
                case eResist.Matter:
                    Matter += valueToDecreaseBy;
                    return Matter;
                case eResist.Natural:
                    Natural += valueToDecreaseBy;
                    return Natural;
                case eResist.Slash:
                    Slash += valueToDecreaseBy;
                    return Slash;
                case eResist.Spirit:
                    Spirit += valueToDecreaseBy;
                    return Spirit;
                case eResist.Thrust:
                    Thrust += valueToDecreaseBy;
                    return Thrust;
                default:
                    return 0;
            }
        }
    }

}
