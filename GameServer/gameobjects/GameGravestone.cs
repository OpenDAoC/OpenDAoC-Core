using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class GameGravestone : GameStaticItem
    {
        // GameGravestone inherits from GameStaticItem, but uses its own database object (DbGravestone).
        // This means _dbWorldObject should not be used in this class and in non-overridden methods from GameStaticItem.

        private static Dictionary<string, HashSet<GameGravestone>> _gravestonesByOwner = new();
        private static Lock _gravestonesLock = new();

        private DbGravestone _dbGravestone;
        private long _xpValue;
        private DateTime _creationTime;

        public static void Create(GamePlayer player, long xpValue)
        {
            GameGravestone gravestone = PruneExcessGravestonesAndGetReusable(player);

            if (gravestone != null)
                gravestone.Delete();
            else
                gravestone = new();

            gravestone.Initialize(player, xpValue);
            gravestone.SaveIntoDatabase(); // Sets InternalID, must be called before AddToWorld.

            if (gravestone.AddToWorld())
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Release.GraveErected"), eChatType.CT_YourDeath, eChatLoc.CL_SystemWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Release.ReturnToPray"), eChatType.CT_YourDeath, eChatLoc.CL_SystemWindow);
            }
        }

        public long Consume()
        {
            long xpValue = _xpValue;
            _xpValue = 0;
            Delete();
            DeleteFromDatabase();
            return xpValue;
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            lock (_gravestonesLock)
            {
                if (_gravestonesByOwner.TryGetValue(OwnerID, out var gravestones))
                    gravestones.Add(this);
                else
                    _gravestonesByOwner[OwnerID] = [this];
            }

            return true;
        }

        public override bool RemoveFromWorld()
        {
            // Always remove from internal cache even if RemoveFromWorld fails.
            lock (_gravestonesLock)
            {
                if (_gravestonesByOwner.TryGetValue(OwnerID, out var gravestones))
                {
                    gravestones.Remove(this);

                    if (gravestones.Count == 0)
                        _gravestonesByOwner.Remove(OwnerID);
                }
            }

            return base.RemoveFromWorld();
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            _dbGravestone = obj as DbGravestone;
            OwnerID = _dbGravestone.OwnerId;
            Name = _dbGravestone.Name;
            X = _dbGravestone.X;
            Y = _dbGravestone.Y;
            Z = _dbGravestone.Z;
            Heading = _dbGravestone.Heading;
            CurrentRegionID = _dbGravestone.Region;
            Model = _dbGravestone.Model;
            _xpValue = _dbGravestone.XpValue;
            _creationTime = _dbGravestone.CreationTime;
            InternalID = obj.ObjectId;
        }

        public override void SaveIntoDatabase()
        {
            if (LoadedFromScript)
                return;

            _dbGravestone ??= new();
            _dbGravestone.OwnerId = OwnerID;
            _dbGravestone.Name = Name;
            _dbGravestone.X = X;
            _dbGravestone.Y = Y;
            _dbGravestone.Z = Z;
            _dbGravestone.Heading = Heading;
            _dbGravestone.Region = CurrentRegionID;
            _dbGravestone.Model = Model;
            _dbGravestone.XpValue = _xpValue;
            _dbGravestone.CreationTime = _creationTime;

            if (InternalID == null)
            {
                GameServer.Database.AddObject(_dbGravestone);
                InternalID = _dbGravestone.ObjectId;
            }
            else
                GameServer.Database.SaveObject(_dbGravestone);
        }

        public override void DeleteFromDatabase()
        {
            if (_dbGravestone == null || !_dbGravestone.IsPersisted)
                return;

            GameServer.Database.DeleteObject(_dbGravestone);
            InternalID = null;
        }

        private void Initialize(GamePlayer player, long xpValue)
        {
            OwnerID = player.ObjectId;
            Name = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGravestone.GameGravestone.Grave", player.Name);
            X = player.X;
            Y = player.Y;
            Z = player.Z;
            Heading = player.Heading;
            CurrentRegionID = player.CurrentRegionID;
            Model = GetGraveRealm(player.Realm);
            _xpValue = xpValue;
            _creationTime = DateTime.Now;
            LoadedFromScript = false;
        }

        private static ushort GetGraveRealm(eRealm realm)
        {
            return realm switch
            {
                eRealm.Albion => 145,
                eRealm.Midgard => 636,
                eRealm.Hibernia => 637,
                _ => 1681,
            };
        }

        public static GameGravestone PruneExcessGravestonesAndGetReusable(GamePlayer player)
        {
            const int MAX_GRAVESTONES_PER_PLAYER = 1;

            do
            {
                GameGravestone oldestGrave = null;
                bool isExcess = false;

                lock (_gravestonesLock)
                {
                    if (_gravestonesByOwner.TryGetValue(player.ObjectId, out var gravestones) && gravestones.Count >= MAX_GRAVESTONES_PER_PLAYER)
                    {
                        DateTime oldestTime = DateTime.MaxValue;

                        foreach (GameGravestone gravestone in gravestones)
                        {
                            if (gravestone._creationTime < oldestTime)
                            {
                                oldestTime = gravestone._creationTime;
                                oldestGrave = gravestone;
                            }
                        }

                        isExcess = gravestones.Count > MAX_GRAVESTONES_PER_PLAYER;
                    }
                }

                if (oldestGrave == null)
                    return null;

                if (isExcess)
                {
                    oldestGrave.Delete();
                    oldestGrave.DeleteFromDatabase();
                }
                else
                    return oldestGrave;
            } while (true);
        }
    }
}
