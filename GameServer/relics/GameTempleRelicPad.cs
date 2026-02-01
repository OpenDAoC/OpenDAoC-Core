using System;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    /// <summary>
    /// Temple Pads NF
    /// Only orginal relics can be stored in temples, 1 relic per temple
    /// </summary>
    public class GameTempleRelicPad : GameRelicPad
    {
        private eRealm _myRealm = eRealm.None;
        private eRelicType _myType = eRelicType.Invalid;

        public GameTempleRelicPad() : base() { }

        #region Identity Overrides

        public override eRealm Realm
        {
            get => _myRealm;
            set => _myRealm = value;
        }

        public override eRelicType PadType => _myType;

        public override ushort Model
        {
            get => 2649; // Invisible Pad
            set => base.Model = value;
        }

        public override int Emblem
        {
            get => base.Emblem;
            set
            {
                base.Emblem = value;
                AssignIdentity(value);
            }
        }

        #endregion

        #region Core Logic

        /// <summary>
        /// Mount logic for Temples in NF
        /// </summary>
        public override bool MountRelic(GameRelic relic, bool returning)
        {
            if (relic == null) return false;

            if (MountedRelics.Count >= 1 && !MountedRelics.Contains(relic))
            {
                return false; 
            }
            if (relic.RelicType != this.PadType)
            {
                return false;
            }
            if (relic.OriginalRealm != this.Realm)
            {
                return false;
            }
            return base.MountRelic(relic, returning);
        }

        public override bool AddToWorld()
        {
            AssignIdentity(Emblem);
            return base.AddToWorld();
        }

        #endregion

        #region Helper

        private void AssignIdentity(int emblem)
        {
            switch (emblem)
            {
                // Strength Relic Pads
                case 1:  _myRealm = eRealm.Albion;   _myType = eRelicType.Strength; break;
                case 2:  _myRealm = eRealm.Midgard;  _myType = eRelicType.Strength; break;
                case 3:  _myRealm = eRealm.Hibernia; _myType = eRelicType.Strength; break;
                
                // Magic Relic Pads
                case 11: _myRealm = eRealm.Albion;   _myType = eRelicType.Magic; break;
                case 12: _myRealm = eRealm.Midgard;  _myType = eRelicType.Magic; break;
                case 13: _myRealm = eRealm.Hibernia; _myType = eRelicType.Magic; break;
                
                default:
                    _myRealm = eRealm.None;
                    _myType = eRelicType.Invalid;
                    break;
            }
        }

        #endregion
    }
}