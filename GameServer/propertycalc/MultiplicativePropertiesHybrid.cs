using System.Collections.Generic;
using System.Threading;

namespace DOL.GS.PropertyCalc
{
    public sealed class MultiplicativePropertiesHybrid : IMultiplicativeProperties
    {
        private readonly Lock _lock = new();
        private Dictionary<int, PropertyEntry> m_properties = new();

        public void Set(int index, object key, double value)
        {
            lock (_lock)
            {
                if (!m_properties.TryGetValue(index, out PropertyEntry entry))
                {
                    entry = new PropertyEntry();
                    m_properties[index] = entry;
                }

                entry.values ??= new();
                entry.values[key] = value;
                entry.CalculateCachedValue();
            }
        }

        public void Remove(int index, object key)
        {
            lock (_lock)
            {
                if (!m_properties.TryGetValue(index, out PropertyEntry entry) || entry.values == null)
                    return;

                entry.values.Remove(key);

                if (entry.values.Count == 0)
                    m_properties.Remove(index);
                else
                    entry.CalculateCachedValue();
            }
        }

        public double Get(int index)
        {
            return m_properties.TryGetValue(index, out PropertyEntry entry) ? entry.cachedValue : 1.0;
        }

        private sealed class PropertyEntry
        {
            public double cachedValue = 1.0;
            public Dictionary<object, double> values;

            public void CalculateCachedValue()
            {
                if (values == null || values.Count == 0)
                {
                    cachedValue = 1.0;
                    return;
                }

                double res = 1.0;

                foreach (double value in values.Values)
                    res *= value;

                cachedValue = res;
            }
        }
    }
}
