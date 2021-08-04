namespace DOL.GS
{
    public class BaseConGameEffect:IECSGameEffect
    {
        public eEffect Type { get; set; }
        public long ExpireTick { get; set; }
        public bool NeverExpire { get; set; }
        public string Name { get; set; }
        
        public GameLiving Owner { get; set; }
        public int Value { get; set; }
        

        public BaseConGameEffect(long expireTick, bool neverExpire, string name, int value, GameLiving owner)
        {
            Type = eEffect.BaseCon;
            ExpireTick = expireTick;
            NeverExpire = neverExpire;
            Name = name;
            Value = value;
            Owner = owner;
        }

    }
}