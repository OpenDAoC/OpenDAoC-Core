﻿using System;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.Expansions.TrialsOfAtlantis;

/// <summary>
/// Ancient bound djinn (Atlantis teleporter).
/// This is the type that stays up all the time.
/// </summary>
public class PermanentDjinn : AncientBoundDjinn
{
    public PermanentDjinn(DjinnStone djinnStone) : base(djinnStone)
    {
        this.Model = VisibleModel;
        this.AddToWorld();

        m_timer = new EmoteTimer(this);
        m_timer.Start(100);
    }

    private EmoteTimer m_timer;

    /// <summary>
    /// Processes events coming from the timer.
    /// </summary>
    /// <param name="e"></param>
    public override void Notify(CoreEvent e)
    {
        if (e is EmoteEvent)
        {
            DoRandomEmote();
            return;
        }

        base.Notify(e);
    }

    /// <summary>
    /// Do a random emote.
    /// </summary>
    private void DoRandomEmote()
    {
        String[] emotes = 
        { 
            "The {0} seems to be making an extremely concerted effort not to start laughing hysterically.",
            "The {0} chuckles a bit to itself and mutters, 'Masters... hah...'",
            "The {0} giggles quietly and whispers to itself, 'That's right, go on, just a little further...'",
            "The {0} sounds as if it's quietly practicing to itself, 'What do you mean they didn't seem to be carrying any valuables?'",
            "The {0} mutters quietly, 'Now where did I put that scepter... oh well, I've dozens more now.'",
            "The {0} chuckles to itself and whispers, 'Oh I'll serve alright... serve just what you deserve.'"
        };

        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
            player.Out.SendMessage(String.Format(emotes[Util.Random(emotes.GetUpperBound(0))], this.Name),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);
    }

    /// <summary>
    /// Permanent djinns in dungeons appear to be slightly
    /// bigger.
    /// </summary>
    protected override byte Size
    {
        get
        {
            return 25;
        }
    }

    /// <summary>
    /// Since they are bigger, they need to hover at
    /// greater height.
    /// </summary>
    protected override int HoverHeight
    {
        get
        {
            return 73;
        }
    }

    /// <summary>
    /// Provides a timer for djinn emotes.
    /// </summary>
    private class EmoteTimer : EcsGameTimerWrapperBase
    {
        private GameObject m_owner;

        /// <summary>
        /// Constructs a new EmoteTimer.
        /// </summary>
        /// <param name="timerOwner">The owner of this timer (the djinn).</param>
        public EmoteTimer(GameObject owner) : base(owner)
        {
            m_owner = owner;
            Interval = 60 * 1000; // 60-second tick.
        }

        /// <summary>
        /// Called on every timer tick.
        /// </summary>
        protected override int OnTick(EcsGameTimer timer)
        {
            m_owner.Notify(new EmoteEvent());
            return Interval;
        }
    }

    /// <summary>
    /// Event for djinn emotes.
    /// </summary>
    /// <author>Aredhel</author>
    private class EmoteEvent : GameLivingEvent
    {
        public EmoteEvent()
            : base("PermanentDjinn.EmoteEvent") { }
    }
}