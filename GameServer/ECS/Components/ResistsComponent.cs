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

        public int GetResist(EResist resist)
        {
            switch (resist)
            {
                case EResist.Body:
                    return Body;
                case EResist.Cold:
                    return Cold;
                case EResist.Crush:
                    return Crush;
                case EResist.Energy:
                    return Energy;
                case EResist.Heat:
                    return Heat;
                case EResist.Matter:
                    return Matter;
                case EResist.Natural:
                    return Natural;
                case EResist.Slash:
                    return Slash;
                case EResist.Spirit:
                    return Spirit;
                case EResist.Thrust:
                    return Thrust;
                default:
                    return 0;
            }
        }

        public void SetResist(EResist resist, int value)
        {
            switch (resist)
            {
                case EResist.Body:
                    this.Body = value;
                    break;
                case EResist.Cold:
                    this.Cold = value;
                    break;
                case EResist.Crush:
                    this.Crush = value;
                    break;
                case EResist.Energy:
                    this.Energy = value;
                    break;
                case EResist.Heat:
                    this.Heat = value;
                    break;
                case EResist.Matter:
                    this.Matter = value;
                    break;
                case EResist.Natural:
                    this.Natural = value;
                    break;
                case EResist.Slash:
                    this.Slash = value;
                    break;
                case EResist.Spirit:
                    this.Spirit = value;
                    break;
                case EResist.Thrust:
                    this.Thrust = value;
                    break;
                default:
                    break;
            }
        }

        public int IncreaseResist(EResist resist, int valueToIncreaseBy)
        {
            switch (resist)
            {
                case EResist.Body:
                    Body += valueToIncreaseBy;
                    return Body;
                case EResist.Cold:
                    Cold += valueToIncreaseBy;
                    return Cold;
                case EResist.Crush:
                    Crush += valueToIncreaseBy;
                    return Crush;
                case EResist.Energy:
                    Energy += valueToIncreaseBy;
                    return Energy;
                case EResist.Heat:
                    Heat += valueToIncreaseBy;
                    return Heat;
                case EResist.Matter:
                    Matter += valueToIncreaseBy;
                    return Matter;
                case EResist.Natural:
                    Natural += valueToIncreaseBy;
                    return Natural;
                case EResist.Slash:
                    Slash += valueToIncreaseBy;
                    return Slash;
                case EResist.Spirit:
                    Spirit += valueToIncreaseBy;
                    return Spirit;
                case EResist.Thrust:
                    Thrust += valueToIncreaseBy;
                    return Thrust;
                default:
                    return 0;
            }
        }

        public int DecreaseStat(EResist resist, int valueToDecreaseBy)
        {
            switch (resist)
            {
                case EResist.Body:
                    Body += valueToDecreaseBy;
                    return Body;
                case EResist.Cold:
                    Cold += valueToDecreaseBy;
                    return Cold;
                case EResist.Crush:
                    Crush += valueToDecreaseBy;
                    return Crush;
                case EResist.Energy:
                    Energy += valueToDecreaseBy;
                    return Energy;
                case EResist.Heat:
                    Heat += valueToDecreaseBy;
                    return Heat;
                case EResist.Matter:
                    Matter += valueToDecreaseBy;
                    return Matter;
                case EResist.Natural:
                    Natural += valueToDecreaseBy;
                    return Natural;
                case EResist.Slash:
                    Slash += valueToDecreaseBy;
                    return Slash;
                case EResist.Spirit:
                    Spirit += valueToDecreaseBy;
                    return Spirit;
                case EResist.Thrust:
                    Thrust += valueToDecreaseBy;
                    return Thrust;
                default:
                    return 0;
            }
        }
    }

}
