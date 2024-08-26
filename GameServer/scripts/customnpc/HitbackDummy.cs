namespace DOL.GS
{
    public class HitbackDummy : GameTrainingDummy {
        public override short MaxSpeedBase => 0;

        public override ushort Heading
        {
            get => base.Heading;
            set => base.Heading = SpawnHeading;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            StopAttack();
            return true;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (!attackComponent.AttackState)
                attackComponent.RequestStartAttack(ad.Attacker);
        }

        public override bool AddToWorld()
        {
            Name = "Hitback Dummy - Right Click to Reset";
            Model = 34;
            Strength = 10;
            DamageFactor = 0.01;
            FixedSpeed = true;
            return base.AddToWorld();
        }
    }
}
