using System;
using DOL.Events;

namespace DOL.GS
{
    /// <summary>
    /// Ancient bound djinn (Atlantis teleporter).
    /// This is the type that is summoned through the djinn stone.
    /// </summary>
    /// <author>Aredhel</author>
    public class SummonedDjinn : AncientBoundDjinn
    {
        private const int SummonSpellEffect = 0x1818;
        private const int InvisibleModel = 0x29a;
        private object m_syncObject = new object();

        /// <summary>
        /// Creates a new SummonedDjinn.
        /// </summary>
        /// <param name="djinnStone"></param>
        public SummonedDjinn(DjinnStone djinnStone)
            : base(djinnStone)
        {
            m_timer = new SummonTimer(this);
        }

        private SummonTimer m_timer;

        /// <summary>
        /// Starts the summon.
        /// </summary>
        public override void Summon()
        {
            lock (m_syncObject)
            {
                if (CurrentRegion == null || IsSummoned)
                    return;

                if (m_timer == null)
                    m_timer = new SummonTimer(this);

                m_summoned = true;
                OnTimerStep(DjinnEvent.Summoning);
            }
        }

        private bool m_summoned = false;

        /// <summary>
        /// Returns true if djinn has been summoned, else false.
        /// </summary>
        public override bool IsSummoned
        {
            get
            {
                lock (m_syncObject)
                    return m_summoned;
            }
        }

        /// <summary>
        /// Interacting with the djinn resets the timer.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            lock (m_syncObject)
                m_timer.Restart();

            return base.Interact(player);
        }

        /// <summary>
        /// Talking to the djinn resets the timer.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, String text)
        {
            lock (m_syncObject)
                m_timer.Restart();

            return base.WhisperReceive(source, text);
        }

        public void OnTimerStep(DOLEvent e)
        {
            if (e == DjinnEvent.Summoning)
            {
                lock (m_syncObject)
                {
                    this.Model = InvisibleModel;
                    this.AddToWorld();
                    m_timer.Start(7, 1000, true, DjinnEvent.Summoned);
                }

                return; // Event is private, no need to propagate.
            }
            else if (e == DjinnEvent.Summoned)
            {
                // Show ourselves.

                lock (m_syncObject)
                {
                    this.Model = VisibleModel;

                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendModelChange(this, this.Model);

                    Say("Greetings, great one.");
                    m_timer.Start(150, 1000, false, DjinnEvent.Vanishing);   // 2.5mins to hiding again.
                }

                return;
            }
            else if (e == DjinnEvent.Vanishing)
            {
                // Go into hiding and show the smoke again.

                lock (m_syncObject)
                {
                    Say("My time here is done.");
                    this.Model = InvisibleModel;

                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendModelChange(this, this.Model);

                    m_timer.Start(5, 1000, true, DjinnEvent.Vanished);
                }

                return;
            }
            else if (e == DjinnEvent.Vanished)
            {
                lock (m_syncObject)
                {
                    this.RemoveFromWorld();
                    m_summoned = false;
                    m_timer.Stop();
                }

                return;
            }
        }

        /// <summary>
        /// Provides a timer for summoning.
        /// </summary>
        private class SummonTimer : ECSGameTimerWrapperBase
        {
            private SummonedDjinn m_owner;
            private int m_maxTicks = 0;
            private int m_ticks = 0;
            private bool m_smoke = false;
            private DjinnEvent m_event;

            /// <summary>
            /// Constructs a new SummonTimer.
            /// </summary>
            /// <param name="owner">The owner of this timer (the djinn).</param>
            public SummonTimer(GameObject owner) : base(owner)
            {
                m_owner = owner as SummonedDjinn;
            }

            private bool m_isRunning = false;

            /// <summary>
            /// Whether the timer is running or not.
            /// </summary>
            public bool IsRunning
            {
                get { return m_isRunning; }
                private set { m_isRunning = value; }
            }

            /// <summary>
            /// Restarts the timer.
            /// </summary>
            public void Restart()
            {
                if (IsRunning)
                {
                    m_ticks = 0;
                    Start();
                }
            }

            /// <summary>
            /// Starts the timer
            /// </summary>
            /// <param name="maxTicks">Number of ticks before the timer stops.</param>
            /// <param name="interval">Time between individual ticks.</param>
            /// <param name="smoke">Whether to show smoke spell effect on each tick or not.</param>
            /// <param name="e">The event to send when timer is stopped.</param>
            public void Start(int maxTicks, int interval, bool smoke, DjinnEvent e)
            {
                m_maxTicks = maxTicks;
                Interval = interval;
                m_smoke = smoke;
                m_event = e;
                m_ticks = 0;
                Start();
                IsRunning = true;
                OnTick(this);
            }

            /// <summary>
            /// Called on every timer tick.
            /// </summary>
            protected override int OnTick(ECSGameTimer timer)
            {
                if (m_ticks++ < m_maxTicks)
                {
                    if (m_smoke)
                    {
                        // Send smoke animation to players in visibility range.

                        foreach (GamePlayer player in m_owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            player.Out.SendSpellEffectAnimation(m_owner, m_owner, SummonSpellEffect, 0, false, 0x01);
                    }
                }
                else
                {
                    // We're done, stop the timer and notify owner.
                    IsRunning = false;
                    m_owner.OnTimerStep(m_event);
                }

                return Interval;
            }
        }

        /// <summary>
        /// Event for summoning/vanishing.
        /// </summary>
        private class DjinnEvent : GameLivingEvent
        {
            protected DjinnEvent(String name) : base(name) { }

            public static readonly DjinnEvent Summoning = new DjinnEvent("DjinnEvent.Summoning");
            public static readonly DjinnEvent Summoned = new DjinnEvent("DjinnEvent.Summoned");
            public static readonly DjinnEvent Vanishing = new DjinnEvent("DjinnEvent.Vanishing");
            public static readonly DjinnEvent Vanished = new DjinnEvent("DjinnEvent.Vanished");
        }
    }
}
