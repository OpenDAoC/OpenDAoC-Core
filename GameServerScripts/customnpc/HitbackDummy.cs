using System;

namespace DOL.GS
{
    public class HitbackDummy : GameTrainingDummy {
        Int32 Damage = 0;
        DateTime StartTime;
        TimeSpan TimePassed;
        Boolean StartCheck = true;

        public override short MaxSpeedBase { get => 0; }

        public override bool FixedSpeed { get => true;}

        public override ushort Heading { get => base.Heading; set => base.Heading = SpawnHeading; }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            Damage = 0;
            StartCheck = true;
            Name = "Hitback Total: 0 DPS: 0";
            this.StopAttack();
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
            Name = "Hitback Total: " + Damage.ToString() +" DPS: " + (Damage / (TimePassed.TotalSeconds + 1)).ToString("0");

            if(!this.attackComponent.AttackState)
                this.attackComponent.StartAttack(ad.Attacker);
            
        }

        public override bool AddToWorld()
        {
            Name = "Hitback Total: 0 DPS: 0";
            GuildName = "Right Click to Reset";
            Model = 34;
            Strength = 10;
            ScalingFactor = 4;
            return base.AddToWorld(); // Finish up and add him to the world.
        }

    }
}
