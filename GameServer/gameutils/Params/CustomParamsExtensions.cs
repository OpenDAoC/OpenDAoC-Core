using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Logging;

namespace DOL.GS
{
    public static class CustomParamsExtensions
    {
        public enum RetrievalResult
        {
            KeyNotFound,
            Success,
            SuccessFromConversion,
            ConversionFailed
        }

        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static RetrievalResult TryGetParamSingle<T>(this ICustomParamsValuable obj, string key, out T result)
        {
            if (obj.CustomParamsDictionary == null)
                throw new InvalidOperationException($"{nameof(obj.CustomParamsDictionary)} is null");

            if (!obj.CustomParamsDictionary.TryGetValue(key, out object entry))
            {
                result = default;
                return RetrievalResult.KeyNotFound;
            }

            if (entry is T typedValue)
            {
                result = typedValue;
                return RetrievalResult.Success;
            }

            // Misuse. Asking for Single, but cached as List<T>.
            // Return first item.
            if (entry is List<T> typedList)
            {
                result = typedList.Count > 0 ? typedList[0] : default;
                return RetrievalResult.Success;
            }

            if (TryConvertAndCacheSingle(obj, key, entry, out result))
                return RetrievalResult.SuccessFromConversion;

            result = default;
            return RetrievalResult.ConversionFailed;
        }

        public static RetrievalResult TryGetParamList<T>(this ICustomParamsValuable obj, string key, out List<T> result)
        {
            if (obj.CustomParamsDictionary == null)
                throw new InvalidOperationException($"{nameof(obj.CustomParamsDictionary)} is null");

            if (!obj.CustomParamsDictionary.TryGetValue(key, out object entry))
            {
                result = default;
                return RetrievalResult.KeyNotFound;
            }

            if (entry is List<T> typedList)
            {
                result = typedList;
                return RetrievalResult.Success;
            }

            // Misuse. Asking for List<T>, but cached as Single.
            // Upgrade to List<T>.
            if (TryConvertAndCacheList(obj, key, entry, out result))
                return RetrievalResult.SuccessFromConversion;

            result = default;
            return RetrievalResult.ConversionFailed;
        }

        public static void PrewarmParamValue<T>(this ICustomParamsValuable obj, string key)
        {
            _ = TryGetParamSingle<T>(obj, key, out _);
        }

        public static void PrewarmParamList<T>(this ICustomParamsValuable obj, string key)
        {
            _ = TryGetParamList<T>(obj, key, out _);
        }

        public static void Init<T>(this ICustomParamsValuable obj, ReadOnlySpan<T> data, Func<T, string> keySelector, Func<T, string> valueSelector)
        {
            if (data.IsEmpty)
            {
                obj.CustomParamsDictionary = [];
                return;
            }

            Dictionary<string, object> dictionary = [];

            foreach (T item in data)
            {
                string key = keySelector(item);
                string value = valueSelector(item);

                if (!dictionary.TryGetValue(key, out object existing))
                    dictionary[key] = value;
                else
                {
                    if (existing is List<string> list)
                        list.Add(value);
                    else if (existing is string singleValue)
                    {
                        List<string> newList = [singleValue, value];
                        dictionary[key] = newList;
                    }
                }
            }

            obj.CustomParamsDictionary = dictionary;
        }

        public static bool IsSuccessfulRetrieval(RetrievalResult result)
        {
            return result is RetrievalResult.Success or RetrievalResult.SuccessFromConversion;
        }

        private static bool TryConvertAndCacheSingle<T>(ICustomParamsValuable obj, string key, object entry, out T result)
        {
            result = default;

            try
            {
                if (entry is List<string> rawList)
                {
                    if (rawList.Count == 0)
                        return false;

                    result = (T) Convert.ChangeType(rawList[0], typeof(T));
                    // Don't overwrite the list.
                    return true;
                }

                if (entry is string rawString)
                {
                    result = (T) Convert.ChangeType(rawString, typeof(T));
                    obj.CustomParamsDictionary[key] = result;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Failed to convert and cache custom param single for key '{key}' on type '{obj.GetType().FullName}'", ex);

                return false;
            }
        }

        private static bool TryConvertAndCacheList<T>(ICustomParamsValuable obj, string key, object entry, out List<T> result)
        {
            result = null;

            try
            {
                if (entry is List<string> rawList)
                {
                    List<T> list = new(rawList.Count);

                    foreach (string item in rawList)
                        list.Add((T) Convert.ChangeType(item, typeof(T)));

                    obj.CustomParamsDictionary[key] = list;
                    result = list;
                    return true;
                }

                if (entry is string rawString)
                {
                    List<T> list = [(T) Convert.ChangeType(rawString, typeof(T))];
                    obj.CustomParamsDictionary[key] = list;
                    result = list;
                    return true;
                }

                if (entry is T singleTyped)
                {
                    List<T> list = [singleTyped];
                    obj.CustomParamsDictionary[key] = list;
                    result = list;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Failed to convert and cache custom param list for key '{key}' on type '{obj.GetType().FullName}'", ex);

                return false;
            }
        }
    }
}
