using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Keeps;

namespace DOL.GS.Keeps
{
    public class GuardCorpseSummoner : GameKeepGuard
    {
        private static int RESPAWN_INTERVAL = 60000;

        public override int MaxHealth => base.MaxHealth * 3;

        public override void StartRespawn()
        {
            if (IsAlive || Brain is IControlledBrain)
                return;

            if (m_respawnTimer == null)
            {
                m_respawnTimer = new(this, new ECSGameTimer.ECSTimerCallback(RespawnTimerCallback));
                m_respawnTimer.Start(RESPAWN_INTERVAL);
            }

            int RespawnTimerCallback(ECSGameTimer timer)
            {
                if (IsAlive || ObjectState is eObjectState.Active)
                    return 0;

                // I guess this loops indefinitely until the keep is level 10?
                // This is wasteful.
                if (Component.Keep.Level < 10)
                    return timer.Interval;

                Health = MaxHealth;
                Mana = MaxMana;
                Endurance = MaxEndurance;
                int origSpawnX = m_spawnPoint.X;
                int origSpawnY = m_spawnPoint.Y;
                X = m_spawnPoint.X;
                Y = m_spawnPoint.Y;
                Z = m_spawnPoint.Z;
                Heading = m_spawnHeading;
                AddToWorld(); // Assumes it's not failing.
                m_spawnPoint.X = origSpawnX;
                m_spawnPoint.Y = origSpawnY;
                return 0;
            }
        }

        public override bool AddToWorld()
        {
            if (Component.Keep.Level < 10)
            {
                StartRespawn();
                return false;
            }

            if (base.AddToWorld())
            {
                Name = "Corpse Summoner";
                Model = 25;
                Size = 60;
                MaxSpeedBase = 0;
                SetOwnBrain(new CorpseSummonerBrain());
                return true;
            }

            return false;
        }
    }
}

namespace DOL.AI.Brain
{
    public class CorpseSummonerBrain : KeepGuardBrain
    {
        protected GuardCorpseSummoner _guardCorpseSummonerBody;

        public override GameNPC Body
        {
            get => _guardCorpseSummonerBody ?? base.Body;
            set
            {
                _guardCorpseSummonerBody = value as GuardCorpseSummoner;
                base.Body = value;
            }
        }

        public CorpseSummonerBrain() : base()
        {
            AggroLevel = 90;
            AggroRange = 1500;
        }

        public override void Think()
        {
            base.Think();

            if (_guardCorpseSummonerBody.Component.Keep != null && _guardCorpseSummonerBody.Component.Keep.Level < 10)
            {
                _guardCorpseSummonerBody.RemoveFromWorld();
                _guardCorpseSummonerBody.StartRespawn();
            }
        }
    }
}
