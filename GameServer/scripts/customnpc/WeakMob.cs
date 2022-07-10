namespace DOL.GS
{
    public class WeakMob : GameNPC {

        public override int MaxHealth { get => 200; set { } }

        public override short Strength { get => 5; set { } }

        public override int RespawnInterval { get => 5000; set { } }
        
        public override bool AddToWorld()
        {
            Name = "badger";
            GuildName = "I'm weak";
            Model = 572;
            Level = 50;
            Flags = 0;
            return base.AddToWorld(); // Finish up and add him to the world.
        }

    }
}
