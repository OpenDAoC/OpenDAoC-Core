using System;

namespace DOL.GS
{
    public class HealDummy : GameTrainingDummy {
        Int32 Healing = 0;
        DateTime StartTime;
        TimeSpan TimePassed;
        Boolean StartCheck = true;

        public override int Health { get => base.MaxHealth / 5; set { } }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            Healing = 0;
            StartCheck = true;
            Name = "Heal Dummy Total: 0 HPS: 0";
            return true;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
                       
        }

        public override int ChangeHealth(GameObject changeSource, eHealthChangeType healthChangeType, int changeAmount)
        {
            if (StartCheck)
            {
                StartTime = DateTime.Now;
                StartCheck = false;
            }

            Healing += changeAmount;
            Name = "Heal Dummy Total: " + Healing.ToString() + " HPS: " + (Healing / (TimePassed.TotalSeconds + 1)).ToString("0");
            return changeAmount;
        }

        public override bool AddToWorld()
        {
            Name = "Heal Dummy";
            GuildName = "Atlas Dummy Union";
            base.ChangeHealth(this, eHealthChangeType.Spell, -200);
            Model = 34;
            return base.AddToWorld(); // Finish up and add him to the world.
        }

    }
}
