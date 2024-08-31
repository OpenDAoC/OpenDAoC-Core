using System;
using DOL.GS;

namespace DOL.Events
{
    public class GainedExperienceEventArgs : EventArgs
    {
        public long ExpBase { get; }
        public long ExpCampBonus { get; }
        public long ExpGroupBonus { get; }
        public long ExpGuildBonus { get; }
        public long ExpBafBonus { get; }
        public long ExpOutpostBonus { get; }
        public long ExpTotal { get; }
        public bool SendMessage { get; }
        public bool AllowMultiply { get; }
        public eXPSource XPSource { get; }

        public GainedExperienceEventArgs(long expBase, long expCampBonus, long expGroupBonus, long expGuildBonus, long expBafBonus, long expOutpostBonus, bool sendMessage, bool allowMultiply, eXPSource xpSource)
        {
            ExpBase = expBase;
            ExpCampBonus = expCampBonus;
            ExpGroupBonus = expGroupBonus;
            ExpGuildBonus = expGuildBonus;
            ExpBafBonus = expBafBonus;
            ExpOutpostBonus = expOutpostBonus;
            ExpTotal = expBase + expCampBonus + expGroupBonus + expGuildBonus + expBafBonus + expOutpostBonus; // Needs to be updated every time a new bonus is added.
            SendMessage = sendMessage;
            AllowMultiply = allowMultiply;
            XPSource = xpSource;
        }
    }
}
