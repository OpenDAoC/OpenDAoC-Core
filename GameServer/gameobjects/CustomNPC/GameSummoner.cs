using DOL.AI;
using DOL.AI.Brain;

namespace DOL.GS
{
    /// <summary>
    /// An NPC which summons a pet based on PetTemplate when health is below SummonThreshold, 
    /// and releases it on death or when it heals to full.
    /// </summary>
    public abstract class GameSummoner : GameNPC
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Percent health remaining to summon pet
        /// </summary>
        abstract public int PetSummonThreshold { get; }

        /// <summary>
        /// Template to base summon on
        /// </summary>
        abstract public NpcTemplate PetTemplate { get; }

        /// <summary>
        /// Level to summon pet at; positive for absolute value, negative for percentage of summoner's level
        /// </summary>
        abstract public byte PetLevel { get; }

        // Optional modifiers.
        virtual public int PetTetherRange { get { return 0; } }
        virtual public byte PetSize {  get { return 0; } }
        virtual public int PetSummonDistance {  get { return 100; } }
        virtual public long PetResummonTime { get { return 60000; } }

        private GameNPC m_pet = null;
        private long m_resummonTime = 0;

        public GameSummoner() : base() { }
        public GameSummoner(ABrain defaultBrain) : base(defaultBrain) { }
        public GameSummoner(INpcTemplate template) : base(template) { }

        /// <summary>
        /// Gets/sets the object health
        /// </summary>
        public override int Health
        {
            get { return base.Health; }
            set
            {
                base.Health = value;

                if (value >= MaxHealth)
                {
                    if (PetSummonThreshold > 100)
                    {
                        // Release and resummon the pet if it's dead
                        if (m_pet != null && !m_pet.IsAlive)
                            ReleasePet();

                        SummonPet();
                    }
                    else
                    {
                        ReleasePet();
                        m_resummonTime = 0;
                    }
                }
            }
        }

        /// <summary>
		/// This living takes damage
		/// </summary>
		/// <param name="ad">AttackData containing damage details</param>
        public override void TakeDamage(AttackData ad)
        {
            // If people kill the pet first, the owner summons a new one when attacked.
            if (Health >= MaxHealth)
                m_resummonTime = 0;

            base.TakeDamage(ad);

            if (!IsAlive)
                ReleasePet();
            else if (HealthPercent < PetSummonThreshold)
            {
                if (m_pet != null && !m_pet.IsAlive && CurrentRegion.Time > m_resummonTime)
                    ReleasePet();

                if (m_pet == null)
                    SummonPet();
            }

            if (m_pet != null && m_pet.IsAlive && !m_pet.InCombat && m_pet.Brain is StandardMobBrain petBrain)
            {
                petBrain.AddToAggroList(ad.Attacker, 1);
                petBrain.Think();
             }
        }

        /// <summary>
        /// Adds the npc to the world
        /// </summary>
        /// <returns>true if the npc has been successfully added</returns>
        public override bool AddToWorld()
        {
            bool ret = base.AddToWorld();

            if (PetSummonThreshold > 100)
                SummonPet();

            return ret;
        }

        /// <summary>
        /// Summon the pet
        /// </summary>
        protected virtual void SummonPet()
        {
            if (PetTemplate != null && PetLevel != 0 && m_pet == null)
            {
                m_pet = new GameNPC(PetTemplate);
                if (m_pet != null)
                {
                    m_pet.CurrentRegion = CurrentRegion;

                    // Summon pet to the left or right of the summoner
                    ushort sideHeading = (ushort)(Heading + 900);
                    if (Util.Random(1) < 1)
                        sideHeading += 1800;
                    Point2D point = GetPointFromHeading(sideHeading, PetSummonDistance);
                    m_pet.X = point.X;
                    m_pet.Y = point.Y;
                    m_pet.Z = Z;

                    m_pet.Heading = Heading;
                    m_pet.Realm = eRealm.None;
                    m_pet.LoadedFromScript = true;
                    m_pet.TetherRange = PetTetherRange;
                    m_pet.RespawnInterval = -1;
                    m_pet.IsWorthReward = false;

                    if (PetSize > 0)
                        m_pet.Size = PetSize;

                    if (PetLevel > 0)
                        m_pet.Level = PetLevel;
                    else
                        m_pet.Level = (byte)(Level * PetLevel / -100);

                    m_pet.AutoSetStats();

                    if (m_pet.Brain is StandardMobBrain petBrain && Brain is StandardMobBrain brain && TargetObject is GameLiving living)
                    {
                        petBrain.CanBAF = false;
                        brain.AddAggroListTo(petBrain);
                    }

                    m_pet.AddToWorld();

                    m_resummonTime = CurrentRegion.Time + PetResummonTime;
                }
            }
        }

        /// <summary>
        /// Release the pet
        /// </summary>
        protected virtual void ReleasePet()
        {
            if (m_pet != null)
            {
                m_pet.RemoveFromWorld();
                m_pet.Delete();
                m_pet = null;
            }
        }
    }
}
