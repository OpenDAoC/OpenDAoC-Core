using System.Collections.Generic;

namespace DOL.GS
{
    public static class ConLevelUtility
    {
        private const int MAX_LEVEL = 127 + 1; // Adding one to be able to use level 0 and so that indexes match entities' levels.

        public static short[,] BuildGrid(List<Dictionary<short, short>> data)
        {
            short[,] grid = new short[MAX_LEVEL, MAX_LEVEL];

            // Add missing data. For higher levels, we'll be using the data from level 50, shifted by one for every level difference.
            Dictionary<short, short> lastKnownData = data[^1];

            for (int i = data.Count, j = 1; i < MAX_LEVEL; i++, j++)
            {
                data.Add(new()
                {
                    { -3, (short) (lastKnownData[-3] + j) },
                    { -2, lastKnownData[-2] },
                    { -1, lastKnownData[-1] },
                    { 0, lastKnownData[0] },
                    { 1, lastKnownData[1] },
                    { 2, lastKnownData[2] }
                });
            }

            for (int i = 0; i < data.Count; i++)
            {
                int index = 0;

                foreach (var item in data[i])
                {
                    for (int j = 0; j < item.Value && index < MAX_LEVEL; j++, index++)
                        grid[i, index] = item.Key;
                }

                // Fill remaining capacity with purple.
                for (; index < MAX_LEVEL; index++)
                    grid[i, index] = 3;
            }

            return grid;
        }

        public static int GetConLevel(short[,] grid, int userLevel, int targetLevel)
        {
            if (userLevel < 0 || userLevel >= grid.GetLength(0))
                return 0;

            if (targetLevel < 0 || targetLevel >= grid.GetLength(1))
                return 0;

            return grid[userLevel, targetLevel];
        }

        public static ConColor GetConColor(int conLevel)
        {
            return (ConColor) conLevel is < ConColor.Grey or > ConColor.Purple ?
                ConColor.Unknown :
                (ConColor) conLevel;
        }
    }

    public static class CharacterConLevelGrid
    {
        private static readonly short[,] _conLevels;

        static CharacterConLevelGrid()
        {
            // One dictionary per level. Key value pairs represent a con level and a span (how many NPC levels from 0).
            // This will be used to populate `_conLevels`.
            // Data is from http://capnbry.net/daoc/concolors.html.
            List<Dictionary<short, short>> data =
            [
                /* 0*/ new() { { 0, 1 }, { 1, 1}, { 2, 1} }, // When forcing player level to 0, a level 0 NPC is grey, everything else is purple.
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
                // From here data is missing.
            ];

            _conLevels = ConLevelUtility.BuildGrid(data);
        }

        public static int GetConLevel(int userLevel, int targetLevel)
        {
            return ConLevelUtility.GetConLevel(_conLevels, userLevel, targetLevel);
        }

        public static ConColor GetConColor(int conLevel)
        {
            return ConLevelUtility.GetConColor(conLevel);
        }
    }

    public static class ItemConLevelGrid
    {
        private static readonly short[,] _conLevels;

        static ItemConLevelGrid()
        {
            // One dictionary per level. Key value pairs represent a con level and a span (how many item levels from 0).
            // This will be used to populate `_conLevels`.
            List<Dictionary<short, short>> data =
            [
                /* 0*/ new() { { 0, 2 }, { 1, 1 }, { 2, 1} },
                /* 1*/ new() { { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 2*/ new() { { -1, 1 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 3*/ new() { { -1, 2 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 4*/ new() { { -2, 1 }, { -1, 2 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 5*/ new() { { -2, 2 }, { -1, 2 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 6*/ new() { { -3, 1 }, { -2, 2 }, { -1, 2 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 7*/ new() { { -3, 2 }, { -2, 2 }, { -1, 2 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 8*/ new() { { -3, 3 }, { -2, 2 }, { -1, 2 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /* 9*/ new() { { -3, 4 }, { -2, 2 }, { -1, 2 }, { 0, 3 }, { 1, 2 }, { 2, 2 } },
                /*10*/ new() { { -3, 4 }, { -2, 2 }, { -1, 3 }, { 0, 3 }, { 1, 2 }, { 2, 3 } },
                /*11*/ new() { { -3, 4 }, { -2, 2 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*12*/ new() { { -3, 4 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*13*/ new() { { -3, 5 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*14*/ new() { { -3, 6 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*15*/ new() { { -3, 7 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*16*/ new() { { -3, 8 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*17*/ new() { { -3, 9 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*18*/ new() { { -3, 10 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*19*/ new() { { -3, 11 }, { -2, 3 }, { -1, 3 }, { 0, 4 }, { 1, 3 }, { 2, 3 } },
                /*20*/ new() { { -3, 11 }, { -2, 3 }, { -1, 4 }, { 0, 4 }, { 1, 3 }, { 2, 4 } },
                /*21*/ new() { { -3, 11 }, { -2, 3 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*22*/ new() { { -3, 11 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*23*/ new() { { -3, 12 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*24*/ new() { { -3, 13 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*25*/ new() { { -3, 14 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*26*/ new() { { -3, 15 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*27*/ new() { { -3, 16 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*28*/ new() { { -3, 17 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*29*/ new() { { -3, 18 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*30*/ new() { { -3, 19 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*31*/ new() { { -3, 20 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*32*/ new() { { -3, 21 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*33*/ new() { { -3, 22 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*34*/ new() { { -3, 23 }, { -2, 4 }, { -1, 4 }, { 0, 5 }, { 1, 4 }, { 2, 4 } },
                /*35*/ new() { { -3, 23 }, { -2, 4 }, { -1, 4 }, { 0, 6 }, { 1, 5 }, { 2, 4 } },
                /*36*/ new() { { -3, 23 }, { -2, 4 }, { -1, 4 }, { 0, 7 }, { 1, 6 }, { 2, 5 } },
                /*37*/ new() { { -3, 23 }, { -2, 5 }, { -1, 4 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*38*/ new() { { -3, 23 }, { -2, 5 }, { -1, 5 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*39*/ new() { { -3, 23 }, { -2, 5 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*40*/ new() { { -3, 23 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*41*/ new() { { -3, 24 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*42*/ new() { { -3, 25 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*43*/ new() { { -3, 26 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*44*/ new() { { -3, 27 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*45*/ new() { { -3, 28 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*46*/ new() { { -3, 29 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*47*/ new() { { -3, 30 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*48*/ new() { { -3, 31 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*49*/ new() { { -3, 32 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } },
                /*50*/ new() { { -3, 33 }, { -2, 6 }, { -1, 6 }, { 0, 7 }, { 1, 6 }, { 2, 6 } }
                // From here data is missing.
            ];

            _conLevels = ConLevelUtility.BuildGrid(data);
        }

        public static int GetConLevel(int userLevel, int targetLevel)
        {
            return ConLevelUtility.GetConLevel(_conLevels, userLevel, targetLevel);
        }

        public static ConColor GetConColor(int conLevel)
        {
            return ConLevelUtility.GetConColor(conLevel);
        }
    }

    public enum ConColor
    {
        Unknown = int.MinValue,
        Grey = -3,
        Green = -2,
        Blue = -1,
        Yellow = 0,
        Orange = 1,
        Red = 2,
        Purple = 3
    }
}
