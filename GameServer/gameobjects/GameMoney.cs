using System.Collections.Generic;

namespace DOL.GS
{
    public class GameMoney: GameStaticItemTimed
    {
        private const string DEFAULT_NAME = "bag of coins";
        private static readonly HashSet<string> _names = [DEFAULT_NAME, "small chest", "large chest", "some copper coins"];

        public long Value { get; set; } // In copper.
        public int Mithril => Money.GetMithril(Value);
        public int Platinum => Money.GetPlatinum(Value);
        public int Gold => Money.GetGold(Value);
        public int Silver => Money.GetSilver(Value);
        public int Copper => Money.GetCopper(Value);

        public GameMoney(long value) : base()
        {
            Level = 0;
            Model = 488;
            Realm = 0;
            Value = value;
            Name = DEFAULT_NAME;
        }

        public GameMoney(long value, GameObject dropper) : this(value)
        {
            X = dropper.X;
            Y = dropper.Y;
            Z = dropper.Z;
            Heading = dropper.Heading;
            CurrentRegion = dropper.CurrentRegion;
        }

        public static bool IsItemMoney(string name)
        {
            return _names.Contains(name);
        }

        public override bool TryAutoPickUp(IGameStaticItemOwner itemOwner)
        {
            lock (_pickUpLock)
            {
                return ObjectState is eObjectState.Active && itemOwner.TryAutoPickUpMoney(this);
            }
        }

        public override TryPickUpResult TryPickUp(GamePlayer source, IGameStaticItemOwner itemOwner)
        {
            lock (_pickUpLock)
            {
                return ObjectState is eObjectState.Active ? itemOwner.TryPickUpMoney(source, this) : TryPickUpResult.FAILED;
            }
        }
    }
}
