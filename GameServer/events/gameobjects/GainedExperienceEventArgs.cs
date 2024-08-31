using System;
using DOL.GS;

namespace DOL.Events
{
    public class GainedExperienceEventArgs : EventArgs
    {
        public long ExpBase { get; }
        public long ExpCampBonus { get; }
        public long ExpGroupBonus { get; }
        public long ExpBafBonus { get; }
        public long ExpOutpostBonus { get; }
        public bool SendMessage { get; }
        public bool AllowMultiply { get; }
        public eXPSource XPSource { get; }

        public GainedExperienceEventArgs(long expBase, long expCampBonus, long expGroupBonus, long expBafBonus, long expOutpostBonus, bool sendMessage, bool allowMultiply, eXPSource xpSource)
        {
            ExpBase = expBase;
            ExpCampBonus = expCampBonus;
            ExpGroupBonus = expGroupBonus;
            ExpBafBonus = expBafBonus;
            ExpOutpostBonus = expOutpostBonus;
            SendMessage = sendMessage;
            AllowMultiply = allowMultiply;
            XPSource = xpSource;
        }
    }
}
