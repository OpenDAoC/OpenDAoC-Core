namespace DOL.GS
{
    public interface IECSGameEffect
    {
        eEffect Type { get; set; }
        long ExpireTime { get; set; }
        string Name { get; set; }
        bool Add { get; set; }
        bool Cancel { get; set; }
    }
}