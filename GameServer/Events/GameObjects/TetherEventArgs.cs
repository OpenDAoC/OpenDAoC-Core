using System;

namespace DOL.Events;

class TetherEventArgs : EventArgs
{
    private int m_seconds;

    public TetherEventArgs(int seconds)
    {
        m_seconds = seconds;
    }

    public int Seconds
    {
        get { return m_seconds; }
    }
}