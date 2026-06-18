using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class GameGravestone : GameStaticItem
    {
        // GameGravestone inherits from GameStaticItem, but uses its own database object (DbGravestone).
        // This means _dbWorldObject should not be used in this class and in non-overridden methods from GameStaticItem.

        private DbGravestone _dbGravestone;
        private long _xpValue;

        public DateTime CreationTime { get; private set; }

        public static void Create(GamePlayer player, long xpValue)
        {
            GameGravestone gravestone = GravestoneService.PruneExcessGravestonesAndGetReusable(player);

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

            GravestoneService.AddGravestone(this);
            return true;
        }

        public override bool RemoveFromWorld()
        {
            // Always remove from internal cache even if RemoveFromWorld fails.
            GravestoneService.RemoveGravestone(this);
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
            CreationTime = _dbGravestone.CreationTime;
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
            _dbGravestone.CreationTime = CreationTime;

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

        public override bool IsVisibleTo(GameObject checkObject)
        {
            if (!base.IsVisibleTo(checkObject))
                return false;

            // Hide gravestones of other players if the player has the HideGraves option enabled.
            return checkObject is not GamePlayer player || !player.DBCharacter.HideGraves || OwnerID == player.ObjectId;
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
            CreationTime = DateTime.Now;
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
    }
}
