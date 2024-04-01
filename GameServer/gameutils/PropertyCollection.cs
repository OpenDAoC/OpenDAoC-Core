using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace DOL.GS
{
    public class PropertyCollection
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ConcurrentDictionary<string, object> _properties = new();

        public T GetProperty<T>(string key)
        {
            return GetProperty(key, default(T));
        }

        public T GetProperty<T>(string key, T @default)
        {
            return GetProperty(key, @default, false);
        }

        public T GetProperty<T>(string key, T @default, bool logged)
        {
            bool exists = _properties.TryGetValue(key, out object value);

            if (!exists)
            {
                if (Log.IsWarnEnabled && logged)
                    Log.Warn($"Property {key} is required but not found, default value {@default} will be used.");

                return @default;
            }

            if (value is not T result)
            {
                if (Log.IsWarnEnabled && logged)
                    Log.Warn($"Property {key} found, but {value} isn't of the type provided {typeof(T)}, default value {@default} will be used.");

                return @default;
            }

            return result;
        }

        public void SetProperty(string key, object value)
        {
            if (value == null)
                _properties.TryRemove(key, out _);
            else
                _properties[key] = value;
        }

        public bool TrySetProperty(string key, object value)
        {
            if (value == null)
            {
                _properties.TryRemove(key, out _);
                return true;
            }

            return _properties.TryAdd(key, value);
        }

        public void RemoveProperty(string key)
        {
            _properties.TryRemove(key, out _);
        }

        public bool TryRemoveProperty(string key, out object value)
        {
            return _properties.TryRemove(key, out value);
        }

        public List<string> GetAllProperties()
        {
            return _properties.Keys.ToList();
        }

        public void RemoveAllProperties()
        {
            _properties.Clear();
        }
    }
}
