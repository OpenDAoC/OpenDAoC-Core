using System;

namespace DOL.GS.PropertyCalc
{
    public interface IPropertyIndexer
    {
        void Clear();
        int this[eProperty index] { get; set; }
        [Obsolete("Use the eProperty indexer instead. This will be removed in a future version.")]
        int this[int index] { get; set; }
    }
}
