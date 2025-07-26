using System.Collections.Generic;
using System.Threading;

namespace DOL.GS.PropertyCalc
{
    public sealed class PropertyIndexer : IPropertyIndexer
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<eProperty, int> _properties = new();

        public void Clear()
        {
            lock (_lock)
            {
                _properties?.Clear();
            }
        }

        public int this[eProperty index]
        {
            get
            {
                lock (_lock)
                {
                    return _properties.TryGetValue(index, out int value) ? value : 0;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value == 0)
                        _properties.Remove(index);
                    else
                        _properties[index] = value;
                }
            }
        }
    }
}
