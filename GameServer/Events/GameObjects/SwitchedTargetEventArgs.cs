using System;
using DOL.GS;

namespace DOL.Events;

public class SwitchedTargetEventArgs : EventArgs
{
    public SwitchedTargetEventArgs(GameObject previousTarget, GameObject newTarget)
    {
        PreviousTarget = previousTarget;
        NewTarget = newTarget;
    }

    public GameObject PreviousTarget { get; private set; }
    public GameObject NewTarget { get; private set; }
}