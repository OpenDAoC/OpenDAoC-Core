using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace DOL.GS
{
    public static class Util
    {
        #region Random

        private static Random _random = System.Random.Shared;

        /// <summary>
        /// Generates a random number between 0..max, inclusive 0 AND max
        /// </summary>
        public static int Random(int max)
        {
            return _random.Next(0, max + 1);
        }

        /// <summary>
        /// Generates a random number between min..max, inclusive min AND max
        /// </summary>
        public static int Random(int min, int max)
        {
            return _random.Next(min, max + 1);
        }

        /// <summary>
        /// Generates a random number between 0.0 inclusive and 1.0 exclusive.
        /// </summary>
        public static double RandomDouble()
        {
            return _random.NextDouble();
        }

        /// <summary>
        /// Generates a random number between 0.0 and 1.0 inclusive, with a granularity of 1.0 / (int.MaxValue - 1).
        /// </summary>
        public static double RandomDoubleIncl()
        {
            return _random.Next() / (double) (int.MaxValue - 1);
        }

        public static bool Chance(int chancePercent)
        {
            return chancePercent > _random.Next(100);
        }

        public static bool Chance(double chancePercent)
        {
            return chancePercent > _random.NextDouble();
        }

        #endregion

        public static string FormatTime(long seconds)
        {
            StringBuilder stringBuilder = new(10);
            long minutes = seconds / 60;

            if (minutes > 0)
            {
                stringBuilder.Append(minutes);
                stringBuilder.Append(':');
                stringBuilder.Append((seconds - minutes * 60).ToString("D2"));
                stringBuilder.Append(" min");
            }
            else
            {
                stringBuilder.Append(seconds);
                stringBuilder.Append(" sec");
            }

            return stringBuilder.ToString();
        }

        #region Strings

        private const char PRIMARY_CSV_SEPARATOR = ';';
        private const char SECONDARY_CSV_SEPARATOR = '-';

        public static List<string> SplitCSV(string str, bool rangeCheck = false)
        {
            if (str == null)
                return null;

            List<string> result = str.Split(new char[] { PRIMARY_CSV_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!rangeCheck)
                return result;

            List<string> result2 = new();

            foreach (string split in result)
            {
                if (split.Contains('-'))
                {
                    if (int.TryParse(split.Split(SECONDARY_CSV_SEPARATOR)[0], out int from) && int.TryParse(split.Split(SECONDARY_CSV_SEPARATOR)[1], out int to))
                    {
                        if (from > to)
                            (from, to) = (to, from);

                        for (int i = from; i <= to; i++)
                            result2.Add(i.ToString());
                    }
                }
                else
                    result2.Add(split);
            }

            return result2;
        }

        /// <summary>
        /// Make a sentence, first letter uppercase and replace all parameters
        /// </summary>
        public static string MakeSentence(string message, params string[] args)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            char[] array = string.Format(message, args).ToCharArray();
            array[0] = char.ToUpper(array[0]);
            return new string(array);
        }

        #endregion
    }

    public static class Extensions
    {
        private static readonly Vector3 _xyMask = new(1.0f, 1.0f, 0.0f);

        public static void Resize<T>(this List<T> list, int size, bool fill = false, T element = default)
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
                list.TrimExcess();
            }
            else if (size > count)
            {
                if (size > list.Capacity)
                    list.Capacity = size; // Creates a new internal array.

                if (fill)
                    list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRange(this Vector3 value, Vector3 target, float range)
        {
            return Vector3.DistanceSquared(value, target) <= range * range;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsXY(this Vector3 a, Vector3 b)
        {
            return (a - b) * _xyMask == Vector3.Zero;
        }

        public static void SwapRemoveAt<T>(this IList<T> list, int index)
        {
            list[index] = list[^1];
            list.RemoveAt(list.Count - 1);
        }
    }
}
