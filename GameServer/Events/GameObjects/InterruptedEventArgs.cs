using System;

namespace Core.GS.Events;

public class InterruptedEventArgs : EventArgs
{
    public InterruptedEventArgs(GameLiving attacker)
    {
        Attacker = attacker;
    }

    public GameLiving Attacker { get; private set; }
}