using System.Collections.Concurrent;
using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// helper class for memory efficient usage of property fields
/// it keeps integer values indexed by integer keys
/// </summary>
public sealed class PropertyIndexer : IPropertyIndexer
{
    private readonly ConcurrentDictionary<int, int> m_propDict = new();

    public int this[int index]
    {
        get => m_propDict.TryGetValue(index, out int val) ? val : 0;
        set => m_propDict[index] = value;
    }

    public int this[EProperty index]
    {
        get => this[(int) index];
        set => this[(int) index] = value;
    }
}