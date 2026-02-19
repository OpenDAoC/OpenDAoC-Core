using System;
using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS
{
    public static class CharacterStatValidator
    {
        public const int PointDistributionBudget = 30;
        private const int CostThreshold1 = 10;
        private const int CostThreshold2 = 15;

        public static bool Validate(DbCoreCharacter character, out int distributedPoints)
        {
            Dictionary<eStat, int> stats = new()
            {
                [eStat.STR] = character.Strength,
                [eStat.DEX] = character.Dexterity,
                [eStat.CON] = character.Constitution,
                [eStat.QUI] = character.Quickness,
                [eStat.INT] = character.Intelligence,
                [eStat.PIE] = character.Piety,
                [eStat.EMP] = character.Empathy,
                [eStat.CHR] = character.Charisma
            };

            return Validate(character, stats, out distributedPoints);
        }

        public static bool Validate(DbCoreCharacter character, Dictionary<eStat, int> stats, out int distributedPoints)
        {
            ICharacterClass charClass = ScriptMgr.FindCharacterClass(character.Class);
            Dictionary<eStat, int> raceStats = GlobalConstants.STARTING_STATS_DICT[(eRace) character.Race];

            return Validate(
                stats,
                raceStats,
                charClass.PrimaryStat,
                charClass.SecondaryStat,
                charClass.TertiaryStat,
                character.Level,
                out distributedPoints);
        }

        public static bool Validate(
            Dictionary<eStat, int> characterStats,
            Dictionary<eStat, int> raceStats,
            eStat primaryStat,
            eStat secondaryStat,
            eStat tertiaryStat,
            int currentLevel,
            out int distributedPoints)
        {
            ArgumentNullException.ThrowIfNull(characterStats);
            ArgumentNullException.ThrowIfNull(raceStats);

            distributedPoints = 0;

            foreach (var entry in raceStats)
            {
                eStat stat = entry.Key;
                int raceAmount = entry.Value;

                if (!characterStats.TryGetValue(stat, out int statAmount))
                    return false;

                int levelAmount = 0;

                for (int lvl = currentLevel; lvl > 5; lvl--)
                {
                    if (primaryStat == stat)
                        levelAmount++;

                    if (secondaryStat == stat && (lvl - 6) % 2 == 0)
                        levelAmount++;

                    if (tertiaryStat == stat && (lvl - 6) % 3 == 0)
                        levelAmount++;
                }

                int minimumRequired = raceAmount + levelAmount;
                int above = statAmount - minimumRequired;

                if (above < 0)
                    return false;

                distributedPoints += above;
                distributedPoints += Math.Max(0, above - CostThreshold1);
                distributedPoints += Math.Max(0, above - CostThreshold2);
            }

            return distributedPoints == PointDistributionBudget;
        }
    }
}
