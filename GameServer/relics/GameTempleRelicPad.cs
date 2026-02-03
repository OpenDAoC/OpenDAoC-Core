using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Events;

namespace DOL.GS
{
    /// <summary>
    /// Temple Pads NF
    /// Only original relics can be stored in temples, 1 relic per temple
    /// </summary>
    public class GameTempleRelicPad : GameRelicPad
    {
        private eRealm _myRealm = eRealm.None;
        private eRelicType _myType = eRelicType.Invalid;
        private ECSGameTimer m_beamTimer;
        private GameNPC m_effectProxy;

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

            // Only the specific relic can go on specific temple
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

            bool success = base.MountRelic(relic, returning);
            if (success)
            {
                StartRelicBeam();
            }
            return success;
        }

        public override void RemoveRelic(GameRelic relic)
        {
            base.RemoveRelic(relic);
            StopRelicBeam();
        }

        public override bool AddToWorld()
        {
            AssignIdentity(Emblem);
            bool success = base.AddToWorld();
            if (success && MountedRelics.Count > 0)
            {
                StartRelicBeam();
            }
            return success;
        }

        public override bool RemoveFromWorld()
        {
            StopRelicBeam();
            return base.RemoveFromWorld();
        }

        #endregion

        #region Effect Logic

        public void StartRelicBeam()
        {
            StopRelicBeam();
            if (m_effectProxy == null)
            {
                m_effectProxy = new GameNPC();
                m_effectProxy.Model = 1;
                m_effectProxy.Size = 100;
                m_effectProxy.Name = "";
                m_effectProxy.Realm = this.Realm;
                m_effectProxy.CurrentRegionID = this.CurrentRegionID;
                m_effectProxy.X = this.X;
                m_effectProxy.Y = this.Y;
                m_effectProxy.Z = this.Z + 50;
                m_effectProxy.Flags = GameNPC.eFlags.PEACE | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.DONTSHOWNAME;
                m_effectProxy.AddToWorld();
            }

            m_beamTimer = new ECSGameTimer(this);
            m_beamTimer.Interval = 500;
            m_beamTimer.Callback = new ECSGameTimer.ECSTimerCallback(SendBeamEffect);
            m_beamTimer.Start();
        }

        public void StopRelicBeam()
        {
            if (m_beamTimer != null)
            {
                m_beamTimer.Stop();
                m_beamTimer = null;
            }

            if (m_effectProxy != null)
            {
                m_effectProxy.RemoveFromWorld();
                m_effectProxy = null;
            }
        }

        private int SendBeamEffect(ECSGameTimer timer)
        {
            GameRelic currentRelic = MountedRelics.Cast<GameRelic>().FirstOrDefault();

            if (currentRelic == null || m_effectProxy == null)
            {
                return 0;
            }

            ushort effectID = 0;
            switch (currentRelic.Realm)
            {
                case eRealm.Albion: effectID = 8010; break;
                case eRealm.Midgard: effectID = 8006; break;
                case eRealm.Hibernia: effectID = 8009; break;
            }

            if (effectID != 0)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player != null && player.Out != null)
                    {
                        player.Out.SendSpellEffectAnimation(m_effectProxy, m_effectProxy, effectID, 0, false, 0x01);
                    }
                }
            }

            return 3500; // Repeat every 3.5 seconds
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