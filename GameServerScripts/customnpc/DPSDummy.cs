using System;

namespace DOL.GS
{
    public class DPSDummy : GameTrainingDummy
    {
        Int32 Damage = 0;
        DateTime StartTime;
        TimeSpan TimePassed;
        Boolean StartCheck = true;

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            Damage = 0;
            StartCheck = true;
            Name = "Total: 0 DPS: 0";
            return true;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (StartCheck)
            {
                StartTime = DateTime.Now;
                StartCheck = false;
            }

            Damage += ad.Damage + ad.CriticalDamage;
            TimePassed = (DateTime.Now - StartTime);
            Name = "Total: " + Damage.ToString() +" DPS: " + (Damage / (TimePassed.TotalSeconds + 1)).ToString("0");
        }

        public override bool AddToWorld()
        {
            Name = "Total: 0 DPS: 0";
            GuildName = "DPS Dummy";
            Model = 34;
            return base.AddToWorld(); // Finish up and add him to the world.
        }

    }
}
