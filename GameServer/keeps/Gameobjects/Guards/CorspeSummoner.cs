using DOL.AI.Brain;

namespace DOL.GS.Keeps
{
    public class GuardCorpseSummoner : GameKeepGuard
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private eRealm m_lastRealm = eRealm.None;


        /// <summary>
        /// Lord needs more health at the moment
        /// </summary>
        public override int MaxHealth
        {
            get
            {
                return base.MaxHealth * 3;
            }
        }


        public virtual void StartModifiedRespawn()
        {
            if (IsAlive) return;
            if (Brain is IControlledBrain)
                return;

            int respawnInt = 60 * 1000;

            if (respawnInt > 0)
            {
                int reloadrespawntimer = 0;
                lock (m_respawnTimerLock)
                {
                    if (m_respawnTimer != null)
                    {
                        reloadrespawntimer = m_respawnTimer.TimeUntilElapsed;
                        if (reloadrespawntimer < 0)
                            reloadrespawntimer = 1;

                        m_respawnTimer.Stop();
                        m_respawnTimer = null;
                    }

                    m_respawnTimer = new ECSGameTimer(this);
                    m_respawnTimer.Callback = new ECSGameTimer.ECSTimerCallback(ModdedRespawnTimerCallback);
                    m_respawnTimer.Start(reloadrespawntimer > 0 ? reloadrespawntimer : respawnInt);
                }
            }
        }
        protected virtual int ModdedRespawnTimerCallback(ECSGameTimer respawnTimer)
        {
            lock (m_respawnTimerLock)
            {
                if (m_respawnTimer != null)
                {
                    m_respawnTimer.Stop();
                    m_respawnTimer = null;
                }
            }

            //DOLConsole.WriteLine("respawn");
            //TODO some real respawn handling
            if (IsAlive) return 0;
            if (ObjectState == eObjectState.Active) return 0;
            if (this.Component.Keep.Level < 10)
            {
                this.StartModifiedRespawn();
                return 0;
            }
            //Heal this mob, move it to the spawnlocation
            Health = MaxHealth;
            Mana = MaxMana;
            Endurance = MaxEndurance;
            int origSpawnX = m_spawnPoint.X;
            int origSpawnY = m_spawnPoint.Y;
            //X=(m_spawnX+Random(750)-350); //new SpawnX = oldSpawn +- 350 coords
            //Y=(m_spawnY+Random(750)-350);	//new SpawnX = oldSpawn +- 350 coords
            X = m_spawnPoint.X;
            Y = m_spawnPoint.Y;
            Z = m_spawnPoint.Z;
            Heading = m_spawnHeading;
            AddToWorld();
            m_spawnPoint.X = origSpawnX;
            m_spawnPoint.Y = origSpawnY;
            return 0;
        }
        public override bool AddToWorld()
        {
            if (this.Component.Keep.Level < 10)
            {
                this.StartModifiedRespawn();
                return false;
            }
            if (base.AddToWorld())
            {
                Name = "Corpse Summoner";
                Model = 25;
                Size = 60;
                MaxSpeedBase = 0;
                var corpseSummonerBrain = new CorpseSummonerBrain();
                SetOwnBrain(corpseSummonerBrain);
                m_lastRealm = Realm;
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
        public CorpseSummonerBrain()
            : base()
        {
            AggroLevel = 90;
            AggroRange = 1500;
        }
        /*public override void Think()
        {
            base.Think();
            if ((Body as GuardCorpseSummoner).Component.Keep != null && (Body as GuardCorpseSummoner).Component.Keep.Level < 10)
            {
                Body.RemoveFromWorld();
                (Body as GuardCorpseSummoner).StartModifiedRespawn();
            }
        }*/
    }
}