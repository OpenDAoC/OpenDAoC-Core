using System.Collections;
using System.Collections.Specialized;
using System.Threading;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// Implements multiplicative properties using HybridDictionary
	/// </summary>
	public sealed class MultiplicativePropertiesHybrid : IMultiplicativeProperties
	{
		private readonly Lock _lock = new();

		private sealed class PropertyEntry
		{
			public double cachedValue = 1.0;
			public HybridDictionary values;
			public void CalculateCachedValue()
			{
				if (values == null)
				{
					cachedValue = 1.0;
					return;
				}

				IDictionaryEnumerator de = values.GetEnumerator();
				double res = 1.0;
				while(de.MoveNext())
				{
					res *= (double)de.Value;
				}
				cachedValue = res;
			}
		}

		private HybridDictionary m_properties = new HybridDictionary();

		/// <summary>
		/// Adds new value, if key exists value will be overwriten
		/// </summary>
		/// <param name="index">The property index</param>
		/// <param name="key">The key used to remove value later</param>
		/// <param name="value">The value added</param>
		public void Set(int index, object key, double value)
		{
			lock (_lock)
			{
				PropertyEntry entry = (PropertyEntry)m_properties[index];
				if (entry == null)
				{
					entry = new PropertyEntry();
					m_properties[index] = entry;
				}

				if (entry.values == null)
					entry.values = new HybridDictionary();

				entry.values[key] = value;
				entry.CalculateCachedValue();
			}
		}

		/// <summary>
		/// Removes stored value
		/// </summary>
		/// <param name="index">The property index</param>
		/// <param name="key">The key use to add the value</param>
		public void Remove(int index, object key)
		{
			lock (_lock)
			{
				PropertyEntry entry = (PropertyEntry)m_properties[index];
				if (entry == null) return;
				if (entry.values == null) return;

				entry.values.Remove(key);

				// remove entry if it's empty
				if (entry.values.Count < 1)
				{
					m_properties.Remove(index);
					return;
				}

				entry.CalculateCachedValue();
			}
		}

		/// <summary>
		/// Gets the property value
		/// </summary>
		/// <param name="index">The property index</param>
		/// <returns>The property value (1.0 = 100%)</returns>
		public double Get(int index)
		{
			PropertyEntry entry = (PropertyEntry)m_properties[index];
			if (entry == null) return 1.0;
			return entry.cachedValue;
		}
	}
}
