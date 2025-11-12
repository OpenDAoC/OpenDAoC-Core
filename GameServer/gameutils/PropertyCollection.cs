using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DOL.GS
{
    public class PropertyCollection
    {
        private static readonly Logging.Logger Log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, object> _properties = new();
        private readonly Lock _lock = new();

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
            bool exists;
            object value;

            lock (_lock)
            {
                exists = _properties.TryGetValue(key, out value);
            }

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
            lock (_lock)
            {
                if (value == null)
                    _properties.Remove(key, out _);
                else
                    _properties[key] = value;
            }
        }

        public bool TrySetProperty(string key, object value)
        {
            lock (_lock)
            {
                if (value == null)
                {
                    _properties.Remove(key, out _);
                    return true;
                }

                return _properties.TryAdd(key, value);
            }
        }

        public void RemoveProperty(string key)
        {
            lock (_lock)
            {
                _properties.Remove(key, out _);
            }
        }

        public bool TryRemoveProperty(string key, out object value)
        {
            lock (_lock)
            {
                return _properties.Remove(key, out value);
            }
        }

        public List<string> GetAllProperties()
        {
            lock (_lock)
            {
                return _properties.Keys.ToList();
            }
        }

        public void RemoveAllProperties()
        {
            lock (_lock)
            {
                _properties.Clear();
            }
        }
    }
}
