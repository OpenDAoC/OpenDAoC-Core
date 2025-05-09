using System;
using System.Collections.Concurrent;

namespace DOL.GS.PropertyCalc
{
    public sealed class PropertyIndexer : IPropertyIndexer
    {
        public void Clear()
        {
            m_propDict.Clear();
        }

        private readonly ConcurrentDictionary<eProperty, int> m_propDict = new();

        public int this[eProperty index]
        {
            get => m_propDict.TryGetValue(index, out int value) ? value : 0;
            set => m_propDict[index] = value;
        }

        [Obsolete("Use the eProperty indexer instead. This will be removed in a future version.")]
        public int this[int index]
        {
            get => this[(eProperty) index];
            set => this[(eProperty) index] = value;
        }
    }
}
