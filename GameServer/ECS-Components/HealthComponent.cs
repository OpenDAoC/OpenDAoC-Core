using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class HealthComponent
    {
        protected int m_health;
        private GameLiving owner;

        public HealthComponent(GameLiving owner)
        {
            this.owner = owner;
        }
       

        /// <summary>
        /// Gets/sets the object health
        /// </summary>
        public int Health
        {
            get { return m_health; }
            set
            {

                int maxhealth = MaxHealth;
                if (value >= maxhealth)
                {
                    m_health = maxhealth;

                    // noret: i see a problem here when players get not RPs after this player was healed to full
                    // either move this to GameNPC or add a check.

                    //We clean all damagedealers if we are fully healed,
                    //no special XP calculations need to be done
                    /*lock (m_xpGainers.SyncRoot)
                    {
                        //DOLConsole.WriteLine(this.Name+": Health=100% -> clear xpgainers");
                        m_xpGainers.Clear();
                    }*/


                    //Fenyn TODO: will need to move experience to its own component
                    //experience component holds the exp value that the entity is worth on death
                    //experience component holds list of all entities that have  damaged the player/are valid for exp gain upon death
                    //grantExp command will grant exp to all entities in the list upon death?
                    //just brainstorming

                }
                else if (value > 0)
                {
                    m_health = value;
                }
                else
                {
                    m_health = 0;
                }

                //Component should only hold data and simple functions. Move regen to a System
                /*
                if (IsAlive && m_health < maxhealth)
                {
                    //should be moved to a RegenerationSystem and RegenerationComponent
                    //StartHealthRegeneration();
                }*/
            }
        }

        public int MaxHealth
        {
            //get { return GetModified(eProperty.MaxHealth); }
            //hard coding 100 max health,  change later
            get { return 100; }
        }

        /// <summary>
		/// returns if this living is alive
		/// </summary>
		public virtual bool IsAlive
        {
            get { return Health > 0; }
        }

        /// <summary>
        /// True if living is low on health, else false.
        /// </summary>
        public virtual bool IsLowHealth
        {
            get
            {
                return (Health < 0.1 * MaxHealth);
            }
        }


        /// <summary>
		/// GameTimer used for restoring hp
		/// </summary>
		//protected RegionTimer m_healthRegenerationTimer;
        /// <summary>
        /// The default frequency of regenerating health in milliseconds
        /// </summary>
        protected const ushort m_healthRegenerationPeriod = 3000;

        
        /// <summary>
        /// Interval for health regeneration tics
        /// </summary>
        protected virtual ushort HealthRegenerationPeriod
        {
            get { return m_healthRegenerationPeriod; }
        }
    }
}
