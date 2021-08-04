namespace DOL.GS
{
    public interface IECSGameEffect
    {
        eEffect Type { get; set; }
        long ExpireTick { get; set; }
        bool NeverExpire { get; set; }
        string Name { get; set; }
        
        GameLiving Owner { get; set; }

        int Value { get; set; }
     
    }
}