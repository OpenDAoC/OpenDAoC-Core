using System;
using System.Reflection;
using DOL.Events;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain
{
	/// <summary>
	/// A retriever type mob is an NPC that is spawned from a boss-like
	/// mob (its master). Upon spawning, the master mob orders the
	/// retriever to make for a certain location; once the retriever
	/// has reached its target it reports back to its master. The player's
	/// task usually is to prevent the retriever from reaching its target,
	/// because bad things may happen should it succeed.
	/// </summary>
	/// <author>Aredhel</author>
    class RetrieverMobBrain : StandardMobBrain
    {
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private enum State { Passive, GettingHelp, Aggressive };
        private State m_state;

        /// <summary>
        /// Mob brain main loop.
        /// </summary>
        public override void Think()
        {
            if (m_state != State.Aggressive) return;
            base.Think();
        }

        private GameNpc m_master = null;

        /// <summary>
        /// The NPC that spawned this retriever.
        /// </summary>
        public GameNpc Master
        {
            get { return m_master; }
			set { m_master = value; }
        }

        /// <summary>
        /// Called whenever the NPC's body sends something to its brain.
        /// </summary>
        /// <param name="e">The event that occured.</param>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event details.</param>
        public override void Notify(DOL.Events.CoreEvent e, object sender, EventArgs args)
        {
            // When we get the WalkTo event we start running towards the target
            // location; once we've arrived we'll tell our master. If someone
            // attacks us before we can get to the target location, we'll do what
            // any other mob would do.

            base.Notify(e, sender, args);
            if (e == GameNpcEvent.WalkTo && m_state == State.Passive)
                m_state = State.GettingHelp;
            else if (e == GameNpcEvent.ArriveAtTarget && m_state == State.GettingHelp)
            {
				if (Master != null && Master.Brain != null)
					Master.Brain.Notify(GameNpcEvent.ArriveAtTarget, this.Body, new EventArgs());
                m_state = State.Aggressive;
            }
            else if (e == GameNpcEvent.TakeDamage)
                m_state = State.Aggressive;
        }
    }
}