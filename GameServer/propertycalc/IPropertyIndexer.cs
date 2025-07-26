namespace DOL.GS.PropertyCalc
{
    public interface IPropertyIndexer
    {
        void Clear();
        int this[eProperty index] { get; set; }
    }
}
