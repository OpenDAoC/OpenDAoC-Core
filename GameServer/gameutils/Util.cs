using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace DOL.GS
{
    public static class Util
    {
        #region Random

        private static Random _random = System.Random.Shared;

        [ThreadStatic]
        private static RandomNumberGenerator _cryptoRandom;

        private static RandomNumberGenerator CryptoRandom
        {
            get
            {
                _cryptoRandom ??= RandomNumberGenerator.Create();
                return _cryptoRandom;
            }
        }

        /// <summary>
        /// Generates a Crypto Strength random number between 0..max inclusive 0 AND exclusive max
        /// </summary>
        public static int CryptoNextInt(int maxValue)
        {
            return CryptoNextInt(0, maxValue);
        }

        /// <summary>
        /// Generates a Crypto Strength random number between min..max inclusive min AND exclusive max
        /// </summary>
        public static int CryptoNextInt(int minValue, int maxValue)
        {
            if (minValue == maxValue)
                return minValue;

            if (minValue > maxValue)
            {
                int swap = minValue;
                minValue = maxValue;
                maxValue = swap;
            }

            long diff = maxValue - minValue;
            byte[] buffer = new byte[4];

            // To prevent endless loop.
            int counter = 0;

            while (true)
            {
                counter++;
                CryptoRandom.GetBytes(buffer);
                uint rand = BitConverter.ToUInt32(buffer, 0);
                long max = 1 + (long) int.MaxValue;
                long remainder = max % diff;

                // Very low chance of getting an endless loop.
                if (rand < max - remainder || counter > 10)
                    return (int) (minValue + rand % diff);
            }
        }

        /// <summary>
        /// Generates a Crypto Strength random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number greater than
        /// or equal to 0.0, and less than 1.0.
        /// </returns>
        public static double CryptoNextDouble()
        {
            byte[] buffer = new byte[4];
            CryptoRandom.GetBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0) / (1.0 + uint.MaxValue);
        }

        public static bool RandomBool()
        {
            return Random(1) == 0;
        }

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

        /// <summary>
        /// returns in chancePercent% cases true
        /// </summary>
        /// <param name="chancePercent">0 .. 100</param>
        /// <returns></returns>
        public static bool Chance(int chancePercent)
        {
            return chancePercent > CryptoNextInt(0, 100);
        }

        /// <summary>
        /// returns in chancePercent% cases true
        /// </summary>
        /// <param name="chancePercent">0.0 .. 1.0</param>
        /// <returns></returns>
        public static bool ChanceDouble(double chancePercent)
        {
            return chancePercent > CryptoNextDouble();
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

        public static string TruncateString(string value, int maxLength)
        {
            return string.IsNullOrEmpty(value) ? value : value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

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

        public static bool IsInRange(this Vector3 value, Vector3 target, float range)
        {
            // SH: Removed Z checks when one of the two Z values is zero(on ground)
            if (value.Z == 0 || target.Z == 0)
                return Vector2.DistanceSquared(value.ToVector2(), target.ToVector2()) <= range * range;
            return Vector3.DistanceSquared(value, target) <= range * range;
        }

        public static Vector2 ToVector2(this Vector3 value)
        {
            return new Vector2(value.X, value.Y);
        }

        public static void SwapRemoveAt<T>(this IList<T> list, int index)
        {
            list[index] = list[^1];
            list.RemoveAt(list.Count - 1);
        }
    }
}
