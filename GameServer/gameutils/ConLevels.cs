using System.Collections.Generic;

namespace DOL.GS
{
    // Assumes NPCs cannot have a level below 0 or above 127.
    // Assumes players cannot have a level below 1 or above 50.
    public static class ConLevels
    {
        private static readonly List<List<short>> _conLevels;

        static ConLevels()
        {
            _conLevels = new(51);

            // One dictionary per player level. Key value pairs represent a con level and a span (how many NPC levels from 0).
            // This will be used to populate `_conLevels`.
            List<Dictionary<short, short>> data =
            [
                /* 0*/ [], // Players shouldn't be level 0. This is here so that we can use the user level directly to access the list.
                /* 1*/ new() { { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 2*/ new() { { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 3*/ new() { { -3, 1 }, { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 4*/ new() { { -3, 2 }, { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 5*/ new() { { -3, 3 }, { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 6*/ new() { { -3, 4 }, { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 7*/ new() { { -3, 5 }, { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 8*/ new() { { -3, 6 }, { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /* 9*/ new() { { -3, 7 }, { -2, 1 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 2, 1 } },
                /*10*/ new() { { -3, 7 }, { -2, 1 }, { -1, 2 }, { 0, 1 }, { 1, 1 }, { 2, 2 } },
                /*11*/ new() { { -3, 7 }, { -2, 1 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*12*/ new() { { -3, 7 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*13*/ new() { { -3, 8 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*14*/ new() { { -3, 9 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*15*/ new() { { -3, 10 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*16*/ new() { { -3, 11 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*17*/ new() { { -3, 12 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*18*/ new() { { -3, 13 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*19*/ new() { { -3, 14 }, { -2, 2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }, { 2, 2 } },
                /*20*/ new() { { -3, 14 }, { -2, 2 }, { -1, 3 }, { 0, 2 }, { 1, 2 }, { 2, 3 } },
                /*21*/ new() { { -3, 14 }, { -2, 2 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*22*/ new() { { -3, 14 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*23*/ new() { { -3, 15 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*24*/ new() { { -3, 16 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*25*/ new() { { -3, 17 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*26*/ new() { { -3, 18 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*27*/ new() { { -3, 19 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*28*/ new() { { -3, 20 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*29*/ new() { { -3, 21 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*30*/ new() { { -3, 22 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*31*/ new() { { -3, 23 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*32*/ new() { { -3, 24 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*33*/ new() { { -3, 25 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*34*/ new() { { -3, 26 }, { -2, 3 }, { -1, 3 }, { 0, 3 }, { 1, 3 }, { 2, 3 } },
                /*35*/ new() { { -3, 26 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 4 }, { 2, 3 } },
                /*36*/ new() { { -3, 26 }, { -2, 3 }, { -1, 3 }, { 0, 5 }, { 1, 5 }, { 2, 4 } },
                /*37*/ new() { { -3, 26 }, { -2, 4 }, { -1, 3 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*38*/ new() { { -3, 26 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*39*/ new() { { -3, 26 }, { -2, 4 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*40*/ new() { { -3, 26 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*41*/ new() { { -3, 27 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*42*/ new() { { -3, 28 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*43*/ new() { { -3, 29 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*44*/ new() { { -3, 30 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*45*/ new() { { -3, 31 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*46*/ new() { { -3, 32 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*47*/ new() { { -3, 33 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*48*/ new() { { -3, 34 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*49*/ new() { { -3, 35 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } },
                /*50*/ new() { { -3, 36 }, { -2, 5 }, { -1, 5 }, { 0, 5 }, { 1, 5 }, { 2, 5 } }
                /*..*/ // Should be filled if we want to support players above level 50.
            ];

            for (int i = 0; i < data.Count; i++)
            {
                List<short> list = new(127);

                foreach (var item in data[i])
                {
                    for (int j = 0; j < item.Value; j++)
                        list.Add(item.Key);
                }

                // Fill remaining capacity with purple.
                for (int j = list.Count; j < list.Capacity; j++)
                    list.Add(3);

                _conLevels.Add(list);
            }
        }

        public static int GetConLevel(int userLevel, int targetLevel)
        {
            if (userLevel < 0 || targetLevel < 0)
                return 0;

            if (userLevel > _conLevels.Count - 1)
                return 0;

            List<short> data = _conLevels[userLevel];

            if (targetLevel > data.Count - 1)
                return 0;

            return data[targetLevel];
        }

        public static ConColor GetConColor(int conLevel)
        {
            return (ConColor) conLevel is < ConColor.GREY or > ConColor.PURPLE ? ConColor.UNKNOWN : (ConColor) conLevel;
        }
    }

    public enum ConColor
    {
        UNKNOWN = int.MinValue,
        GREY = -3,
        GREEN = -2,
        BLUE = -1,
        YELLOW = 0,
        ORANGE = 1,
        RED = 2,
        PURPLE = 3
    }
}
