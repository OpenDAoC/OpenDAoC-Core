using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Language;
using DOL.Logging;

namespace DOL.GS.Housing
{
    public abstract class House : Point3D
    {
        protected static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        public const int MAX_VAULT_COUNT = 8; // Must not be bigger than what `eInventorySlot` allows.

        protected readonly DbHouse _databaseItem;
        protected GameConsignmentMerchant _consignmentMerchant;

        public override int X { get => _databaseItem.X; set => _databaseItem.X = value; }
        public override int Y { get => _databaseItem.Y; set => _databaseItem.Y = value; }
        public override int Z { get => _databaseItem.Z; set => _databaseItem.Z = value; }

        public DbHouse DatabaseItem => _databaseItem;
        public int HouseNumber { get => _databaseItem.HouseNumber; set => _databaseItem.HouseNumber = value; }
        public string OwnerID { get => _databaseItem.OwnerID; set => _databaseItem.OwnerID = value; }
        public int Model { get => _databaseItem.Model; set => _databaseItem.Model = value; }
        public int Emblem { get => _databaseItem.Emblem; set => _databaseItem.Emblem = value; }
        public int PorchRoofColor { get => _databaseItem.PorchRoofColor; set => _databaseItem.PorchRoofColor = value; }
        public int PorchMaterial { get => _databaseItem.PorchMaterial; set => _databaseItem.PorchMaterial = value; }
        public bool Porch { get => _databaseItem.Porch; set => _databaseItem.Porch = value; }
        public bool IndoorGuildBanner { get => _databaseItem.IndoorGuildBanner; set => _databaseItem.IndoorGuildBanner = value; }
        public bool IndoorGuildShield { get => _databaseItem.IndoorGuildShield; set => _databaseItem.IndoorGuildShield = value; }
        public bool OutdoorGuildBanner { get => _databaseItem.OutdoorGuildBanner; set => _databaseItem.OutdoorGuildBanner = value; }
        public bool OutdoorGuildShield { get => _databaseItem.OutdoorGuildShield; set => _databaseItem.OutdoorGuildShield = value; }
        public int RoofMaterial { get => _databaseItem.RoofMaterial; set => _databaseItem.RoofMaterial = value; }
        public int DoorMaterial { get => _databaseItem.DoorMaterial; set => _databaseItem.DoorMaterial = value; }
        public int WallMaterial { get => _databaseItem.WallMaterial; set => _databaseItem.WallMaterial = value; }
        public int TrussMaterial { get => _databaseItem.TrussMaterial; set => _databaseItem.TrussMaterial = value; }
        public int WindowMaterial { get => _databaseItem.WindowMaterial; set => _databaseItem.WindowMaterial = value; }
        public int Rug1Color { get => _databaseItem.Rug1Color; set => _databaseItem.Rug1Color = value; }
        public int Rug2Color { get => _databaseItem.Rug2Color; set => _databaseItem.Rug2Color = value; }
        public int Rug3Color { get => _databaseItem.Rug3Color; set => _databaseItem.Rug3Color = value; }
        public int Rug4Color { get => _databaseItem.Rug4Color; set => _databaseItem.Rug4Color = value; }
        public DateTime LastPaid { get => _databaseItem.LastPaid; set => _databaseItem.LastPaid = value; }
        public long KeptMoney { get => _databaseItem.KeptMoney; set => _databaseItem.KeptMoney = value; }
        public bool NoPurge { get => _databaseItem.NoPurge; set => _databaseItem.NoPurge = value; }
        public int UniqueID { get; set; }
        public ushort RegionID { get => _databaseItem.RegionID; set => _databaseItem.RegionID = value; }
        public ushort Heading { get => (ushort)_databaseItem.Heading; set => _databaseItem.Heading = value; }
        public string Name { get => _databaseItem.Name; set => _databaseItem.Name = value; }
        public GameConsignmentMerchant ConsignmentMerchant { get => _consignmentMerchant; set => _consignmentMerchant = value; }

        public abstract IReadOnlyDictionary<int, IndoorItem> IndoorItems { get; }
        public abstract IReadOnlyDictionary<int, OutdoorItem> OutdoorItems { get; }
        public abstract IReadOnlyDictionary<uint, DbHouseHookPointItem> HousePointItems { get; }
        public abstract IReadOnlyDictionary<int, GameHouseVault> HouseVaults { get; }
        public abstract IReadOnlyDictionary<int, DbHouseCharsXPerms> CharXPermissions { get; }
        public abstract IEnumerable<KeyValuePair<int, DbHouseCharsXPerms>> HousePermissions { get; }
        public abstract IReadOnlyDictionary<int, DbHousePermissions> PermissionLevels { get; }

        public abstract bool AddIndoorItem(int key, IndoorItem item);
        public abstract bool RemoveIndoorItem(int key);
        public abstract void ClearAndDeleteIndoorItems();
        public abstract bool AddOutdoorItem(int key, OutdoorItem item);
        public abstract bool RemoveOutdoorItem(int key);
        public abstract void ClearAndDeleteOutdoorItems();
        public abstract bool AddHousePointItem(uint key, DbHouseHookPointItem item);
        public abstract bool RemoveHousePointItem(uint key);
        public abstract void ClearAndDeleteHousePointItems();
        public abstract bool AddHouseVault(int key, GameHouseVault vault);
        public abstract bool RemoveHouseVault(int key);
        public abstract void ClearHouseVaults();

        // Required implementations that depend on db context.
        public abstract void InitializePermissionLevels();
        public abstract bool AddPermission(GamePlayer player, PermissionType permType, int permLevel);
        public abstract bool AddPermission(string targetName, PermissionType permType, int permLevel);
        public abstract void RemovePermission(int slot);
        public abstract void ClearAndDeletePermissions();
        public abstract void AdjustPermissionSlot(int slot, int newPermLevel);
        public abstract void SaveIntoDatabase();
        public abstract void LoadFromDatabase();
        public abstract bool AddPorch();
        public abstract bool RemovePorch();
        public abstract bool AddConsignment(long startValue);
        public abstract bool RemoveConsignmentMerchant();
        public abstract void PickUpConsignmentMerchant(GamePlayer player);
        public abstract void Edit(GamePlayer player, List<int> changes);
        public abstract bool FillHookPoint(uint position, string templateID, ushort heading, int index);
        public abstract void EmptyHookPoint(GamePlayer player, GameObject obj, bool addToInventory = true);
        public abstract int GetAvailableVaultSlot();

        public eRealm Realm
        {
            get
            {
                if (Model < 5)
                    return eRealm.Albion;

                if (Model < 9)
                    return eRealm.Midgard;

                return eRealm.Hibernia;
            }
        }

        public bool IsOccupied
        {
            get
            {
                foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(RegionID, X, Y, 25000, WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player.CurrentHouse == this && player.InHouse)
                        return true;
                }

                return false;
            }
        }

        public GameLocation OutdoorJumpPoint
        {
            get
            {
                double angle = Heading * (Math.PI * 2 / 360);
                int x = (int) (X + (0 * Math.Cos(angle) + 500 * Math.Sin(angle)));
                int y = (int) (Y - (500 * Math.Cos(angle) - 0 * Math.Sin(angle)));
                ushort heading = (ushort) ((Heading < 180 ? Heading + 180 : Heading - 180) / 0.08789);
                return new("Housing", RegionID, x, y, Z, heading);
            }
        }

        protected House(DbHouse dbHouse)
        {
            _databaseItem = dbHouse ?? throw new ArgumentNullException(nameof(dbHouse));
        }

        public void SendUpdate()
        {
            foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(RegionID, this, HousingConstants.HouseViewingDistance))
            {
                player.Out.SendHouse(this);
                player.Out.SendGarden(this);
            }

            foreach (GamePlayer player in GetAllPlayersInHouse())
                player.Out.SendEnterHouse(this);
        }

        public void Enter(GamePlayer player)
        {
            List<GamePlayer> list = GetAllPlayersInHouse();

            if (list.Count == 0)
            {
                foreach (GamePlayer pl in WorldMgr.GetPlayersCloseToSpot(RegionID, this, HousingConstants.HouseViewingDistance))
                    pl.Out.SendHouseOccupied(this, true);
            }

            ChatUtil.SendSystemMessage(player, "House.Enter.EnteringHouse", HouseNumber);
            player.Out.SendEnterHouse(this);
            player.Out.SendFurniture(this);

            player.InHouse = true;
            player.CurrentHouse = this;

            ushort heading = 0;

            switch (Model)
            {
                case 1: player.MoveTo(RegionID, X + 80, Y + 100, 25025, heading); break;
                case 2: player.MoveTo(RegionID, X - 260, Y + 100, 24910, heading); break;
                case 3: player.MoveTo(RegionID, X - 200, Y + 100, 24800, heading); break;
                case 4: player.MoveTo(RegionID, X - 350, Y - 30, 24660, heading); break;
                case 5: player.MoveTo(RegionID, X + 230, Y - 480, 25100, heading); break;
                case 6: player.MoveTo(RegionID, X - 80, Y - 660, 24700, heading); break;
                case 7: player.MoveTo(RegionID, X - 80, Y - 660, 24700, heading); break;
                case 8: player.MoveTo(RegionID, X - 90, Y - 625, 24670, heading); break;
                case 9: player.MoveTo(RegionID, X + 400, Y - 160, 25150, heading); break;
                case 10: player.MoveTo(RegionID, X + 400, Y - 80, 25060, heading); break;
                case 11: player.MoveTo(RegionID, X + 400, Y - 60, 24900, heading); break;
                case 12: player.MoveTo(RegionID, X, Y - 620, 24595, heading); break;
                default: player.MoveTo(RegionID, X, Y, 25022, heading); break;
            }

            ChatUtil.SendSystemMessage(player, "House.Enter.EnteredHouse", HouseNumber);
        }

        public void Exit(GamePlayer player, bool silent)
        {
            player.MoveTo(OutdoorJumpPoint);

            if (!silent)
                ChatUtil.SendSystemMessage(player, "House.Exit.LeftHouse", HouseNumber);

            player.Out.SendExitHouse(this);

            List<GamePlayer> list = GetAllPlayersInHouse();

            if (list.Count == 0)
            {
                foreach (GamePlayer pl in WorldMgr.GetPlayersCloseToSpot(RegionID, this, HousingConstants.HouseViewingDistance))
                    pl.Out.SendHouseOccupied(this, false);
            }
        }

        public void SendHouseInfo(GamePlayer player)
        {
            int level = Model - (Model - 1) / 4 * 4;
            TimeSpan due = LastPaid.AddDays(ServerProperties.Properties.RENT_DUE_DAYS).AddHours(1) - DateTime.Now;

            List<string> text = new()
            {
                LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Owner", Name),
                LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Lotnum", HouseNumber),
                level > 0 ? LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Level", level) : LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Level", "Lot"),
                " ",
                LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Lockbox", Money.GetString(KeptMoney)),
                LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.RentalPrice", Money.GetString(HouseMgr.GetRentByModel(Model))),
                LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.MaxLockbox", Money.GetString(HouseMgr.GetRentByModel(Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
            };

            if (ServerProperties.Properties.RENT_DUE_DAYS > 0)
                text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.RentDueIn", due.Days, due.Hours));
            else
                text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.RentDueIn", "No Rent! 0", "0"));

            text.Add(" ");

            if (player.Client.Account.PrivLevel > (int)ePrivLevel.Player)
            {
                text.Add("GM: Model: " + Model);
                text.Add("GM: Realm: " + GlobalConstants.RealmToName(Realm));
                text.Add("GM: Last Paid: " + LastPaid);
                text.Add(" ");
            }

            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Porch"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.PorchEnabled", Porch ? "Y" : "N"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.PorchRoofColor", PorchRoofColor));
            text.Add(" ");
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.ExteriorMaterials"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.RoofMaterial", RoofMaterial));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.WallMaterial", WallMaterial));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.DoorMaterial", DoorMaterial));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.TrussMaterial", TrussMaterial));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.PorchMaterial", PorchMaterial));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.WindowMaterial", WindowMaterial));
            text.Add(" ");
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.ExteriorUpgrades"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.OutdoorGuildBanner", OutdoorGuildBanner ? "Y" : "N"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.OutdoorGuildShield", OutdoorGuildShield ? "Y" : "N"));
            text.Add(" ");
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.InteriorUpgrades"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.IndoorGuildBanner", IndoorGuildBanner ? "Y" : "N"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.IndoorGuildShield", IndoorGuildShield ? "Y" : "N"));
            text.Add(" ");
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.InteriorCarpets"));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Rug1Color", Rug1Color));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Rug2Color", Rug2Color));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Rug3Color", Rug3Color));
            text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Rug4Color", Rug4Color));

            player.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.HouseOwner", Name), text);
        }

        public int GetPorchAndGuildEmblemFlags()
        {
            int flag = 0;

            if (Porch)
                flag |= 1;

            if (OutdoorGuildBanner)
                flag |= 2;

            if (OutdoorGuildShield)
                flag |= 4;

            return flag;
        }

        public int GetGuildEmblemFlags()
        {
            int flag = 0;

            if (IndoorGuildBanner)
                flag |= 1;

            if (IndoorGuildShield)
                flag |= 2;

            return flag;
        }

        public List<GamePlayer> GetAllPlayersInHouse()
        {
            List<GamePlayer> players = GameLoop.GetListForTick<GamePlayer>();

            foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(RegionID, X, Y, 25000, WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player.CurrentHouse == this && player.InHouse)
                    players.Add(player);
            }

            return players;
        }

        public static bool AddNewOffset(DbHouseHookPointOffset HookPointOffset)
        {
            if (HookPointOffset.HookpointID <= HousingConstants.MaxHookpointLocations)
            {
                HousingConstants.RelativeHookpointsCoords[HookPointOffset.HouseModel][HookPointOffset.HookpointID] =
                [
                    HookPointOffset.X,
                    HookPointOffset.Y,
                    HookPointOffset.Z,
                    HookPointOffset.Heading
                ];

                return true;
            }

            if (log.IsErrorEnabled)
                log.Error($"[Housing]: HouseHookPointOffset exceeds array size.  Model {HookPointOffset.HouseModel}, hook point {HookPointOffset.HookpointID}");

            return false;
        }

        public static void LoadHookPointOffsets()
        {
            for (int i = HousingConstants.MaxHouseModel; i > 0; i--)
            {
                for (int j = 1; j < HousingConstants.RelativeHookpointsCoords[i].Length; j++)
                    HousingConstants.RelativeHookpointsCoords[i][j] = null;
            }

            IList<DbHouseHookPointOffset> hookPointOffsets = GameServer.Database.SelectAllObjects<DbHouseHookPointOffset>();

            foreach (DbHouseHookPointOffset hookPointOffset in hookPointOffsets)
                AddNewOffset(hookPointOffset);
        }

        public Point3D GetHookPointLocation(uint n)
        {
            if (n > HousingConstants.MaxHookpointLocations)
                return null;

            int[] coords = HousingConstants.RelativeHookpointsCoords[Model][n];
            return coords == null ? null : new(X + coords[0], Y + coords[1], 25000 + coords[2]);
        }

        protected int GetHookPointPosition(int objX, int objY)
        {
            for (int i = 0; i < HousingConstants.MaxHookpointLocations; i++)
            {
                if (HousingConstants.RelativeHookpointsCoords[Model][i] != null)
                {
                    if (HousingConstants.RelativeHookpointsCoords[Model][i][0] + X == objX &&
                        HousingConstants.RelativeHookpointsCoords[Model][i][1] + Y == objY)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public ushort GetHookPointHeading(uint n)
        {
            if (n > HousingConstants.MaxHookpointLocations)
                return 0;

            int[] coords = HousingConstants.RelativeHookpointsCoords[Model][n];
            return (ushort) (coords == null ? 0 : (Heading + coords[3]));
        }

        private DbHouseCharsXPerms GetPlayerPermissions(GamePlayer player)
        {
            if (player == null)
                return null;

            var charPermissions = CharXPermissions.Values.Where(cp => cp.TargetName == player.Name && (PermissionType) cp.PermissionType is PermissionType.Player);

            if (charPermissions.Any())
                return charPermissions.First();

            var acctPermissions = CharXPermissions.Values.Where(cp => cp.TargetName == player.Client.Account.Name && (PermissionType) cp.PermissionType is PermissionType.Account);

            if (acctPermissions.Any())
                return acctPermissions.First();

            if (player.Guild != null)
            {
                var guildPermissions = CharXPermissions.Values.Where(cp => player.Guild.Name == cp.TargetName && (PermissionType) cp.PermissionType is PermissionType.Guild);

                if (guildPermissions.Any())
                    return guildPermissions.First();
            }

            var allPermissions = CharXPermissions.Values.Where(cp => cp.TargetName == "All");
            return allPermissions.FirstOrDefault();
        }

        private bool HasAccess(GamePlayer player, Func<DbHousePermissions, bool> accessExpression)
        {
            if (player == null)
                return false;

            if (HasOwnerPermissions(player))
                return true;

            DbHousePermissions housePermissions = GetPermissionLevel(player);
            return housePermissions != null && accessExpression(housePermissions);
        }

        private DbHousePermissions GetPermissionLevel(GamePlayer player)
        {
            DbHouseCharsXPerms permissions = GetPlayerPermissions(player);
            return permissions == null ?  null : GetPermissionLevel(permissions);
        }

        private DbHousePermissions GetPermissionLevel(DbHouseCharsXPerms charPerms)
        {
            return charPerms == null ? null : GetPermissionLevel(charPerms.PermissionLevel);
        }

        private DbHousePermissions GetPermissionLevel(int permissionLevel)
        {
            PermissionLevels.TryGetValue(permissionLevel, out DbHousePermissions permissions);
            return permissions;
        }

        public bool HasOwnerPermissions(GamePlayer player)
        {
            if (player == null)
                return false;

            if ((ePrivLevel) player.Client.Account.PrivLevel is ePrivLevel.Admin)
                return true;

            if (!_databaseItem.GuildHouse)
            {
                if (_databaseItem.OwnerID == player.ObjectId)
                    return true;

                IEnumerable<DbCoreCharacter> charsOnAccount = player.Client.Account.Characters.Where(chr => chr.ObjectId == _databaseItem.OwnerID);

                if (charsOnAccount.Any())
                    return true;
            }

            return player.Guild != null && OwnerID == player.Guild.GuildID && player.Guild.HasRank(player, Guild.eRank.Leader);
        }

        public bool CanEmptyHookPoint(GamePlayer player)
        {
            return HasOwnerPermissions(player);
        }

        public bool CanEnterHome(GamePlayer player)
        {
            return HasAccess(player, cp => cp.CanEnterHouse);
        }

        public bool CanUseVault(GamePlayer player, GameHouseVault vault, VaultPermissions vaultPerms)
        {
            if (player == null)
                return false;

            if (HasOwnerPermissions(player))
                return true;

            DbHousePermissions housePermissions = GetPermissionLevel(player);

            if (housePermissions == null)
                return false;

            VaultPermissions activeVaultPermissions = VaultPermissions.None;

            switch (vault.Index)
            {
                case 0:
                case 4:
                {
                    activeVaultPermissions = (VaultPermissions) housePermissions.Vault1;
                    break;
                }
                case 1:
                case 5:
                {
                    activeVaultPermissions = (VaultPermissions) housePermissions.Vault2;
                    break;
                }
                case 2:
                case 6:
                {
                    activeVaultPermissions = (VaultPermissions) housePermissions.Vault3;
                    break;
                }
                case 3:
                case 7:
                {
                    activeVaultPermissions = (VaultPermissions) housePermissions.Vault4;
                    break;
                }
            }

            ChatUtil.SendDebugMessage(player, $"Vault permissions = {activeVaultPermissions & vaultPerms} for vault index {vault.Index}");
            return (activeVaultPermissions & vaultPerms) > 0;
        }

        public bool CanUseConsignmentMerchant(GamePlayer player, ConsignmentPermissions consignPerms)
        {
            if (player == null)
                return false;

            if (HasOwnerPermissions(player))
                return true;

            DbHousePermissions housePermissions = GetPermissionLevel(player);
            return housePermissions != null && ((ConsignmentPermissions) housePermissions.ConsignmentMerchant & consignPerms) > 0;
        }

        public bool CanChangeInterior(GamePlayer player, DecorationPermissions interiorPerms)
        {
            if (player == null)
                return false;

            if (HasOwnerPermissions(player) || player.Client.Account.PrivLevel > 1)

                return true;

            DbHousePermissions housePermissions = GetPermissionLevel(player);
            return housePermissions != null && ((DecorationPermissions) housePermissions.ChangeInterior & interiorPerms) > 0;
        }

        public bool CanChangeGarden(GamePlayer player, DecorationPermissions gardenPerms)
        {
            if (player == null)
                return false;

            if (HasOwnerPermissions(player) || player.Client.Account.PrivLevel > 1)
                return true;

            DbHousePermissions housePermissions = GetPermissionLevel(player);
            return housePermissions != null && ((DecorationPermissions) housePermissions.ChangeGarden & gardenPerms) > 0;
        }

        public bool CanChangeExternalAppearance(GamePlayer player)
        {
            return HasAccess(player, cp => cp.CanChangeExternalAppearance);
        }

        public bool CanBanish(GamePlayer player)
        {
            return HasAccess(player, cp => cp.CanBanish);
        }

        public bool CanBindInHouse(GamePlayer player)
        {
            return HasAccess(player, cp => cp.CanBindInHouse);
        }

        public bool CanPayRent(GamePlayer player)
        {
            return HasAccess(player, cp => cp.CanPayRent);
        }

        public bool CanUseMerchants(GamePlayer player)
        {
            return HasAccess(player, cp => cp.CanUseMerchants);
        }

        public bool CanUseTools(GamePlayer player)
        {
            return HasAccess(player, cp => cp.CanUseTools);
        }
    }

    public class GameHouse : House
    {
        private readonly ConcurrentDictionary<int, IndoorItem> _indoorItems = new();
        private readonly ConcurrentDictionary<int, OutdoorItem> _outdoorItems = new();
        private readonly ConcurrentDictionary<uint, DbHouseHookPointItem> _housePointItems = new();
        private readonly ConcurrentDictionary<int, GameHouseVault> _houseVaults = new();
        private readonly ConcurrentDictionary<int, DbHouseCharsXPerms> _housePermissions = new();
        private readonly ConcurrentDictionary<int, DbHousePermissions> _permissionLevels = new();

        public override IReadOnlyDictionary<int, IndoorItem> IndoorItems => _indoorItems;
        public override IReadOnlyDictionary<int, OutdoorItem> OutdoorItems => _outdoorItems;
        public override IReadOnlyDictionary<uint, DbHouseHookPointItem> HousePointItems => _housePointItems;
        public override IReadOnlyDictionary<int, GameHouseVault> HouseVaults => _houseVaults;
        public override IReadOnlyDictionary<int, DbHouseCharsXPerms> CharXPermissions => _housePermissions;
        public override IEnumerable<KeyValuePair<int, DbHouseCharsXPerms>> HousePermissions => _housePermissions.OrderBy(entry => entry.Value.CreationTime);
        public override IReadOnlyDictionary<int, DbHousePermissions> PermissionLevels => _permissionLevels;

        public GameHouse(DbHouse house) : base(house) { }

        public override bool AddIndoorItem(int key, IndoorItem item)
        {
            return _indoorItems.TryAdd(key, item);
        }

        public override bool RemoveIndoorItem(int key)
        {
            return _indoorItems.TryRemove(key, out _);
        }

        public override void ClearAndDeleteIndoorItems()
        {
            IList<DbHouseIndoorItem> indoorItems = DOLDB<DbHouseIndoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
            GameServer.Database.DeleteObject(indoorItems);
            _indoorItems.Clear();
        }

        public override bool AddOutdoorItem(int key, OutdoorItem item)
        {
            return _outdoorItems.TryAdd(key, item);
        }

        public override bool RemoveOutdoorItem(int key)
        {
            return _outdoorItems.TryRemove(key, out _);
        }

        public override void ClearAndDeleteOutdoorItems()
        {
            IList<DbHouseOutdoorItem> outdoorItems = DOLDB<DbHouseOutdoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
            GameServer.Database.DeleteObject(outdoorItems);
            _outdoorItems.Clear();
        }

        public override bool AddHousePointItem(uint key, DbHouseHookPointItem item)
        {
            return _housePointItems.TryAdd(key, item);
        }

        public override bool RemoveHousePointItem(uint key)
        {
            return _housePointItems.TryRemove(key, out _);
        }

        public override void ClearAndDeleteHousePointItems()
        {
            IList<DbHouseHookPointItem> housePointItems = DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
            GameServer.Database.DeleteObject(housePointItems);

            foreach (DbHouseHookPointItem item in _housePointItems.Values)
                (item.GameObject as GameObject)?.Delete();

            _housePointItems.Clear();
        }

        public override bool AddHouseVault(int key, GameHouseVault vault)
        {
            return _houseVaults.TryAdd(key, vault);
        }

        public override bool RemoveHouseVault(int key)
        {
            return _houseVaults.TryRemove(key, out _);
        }

        public override void ClearHouseVaults()
        {
            _houseVaults.Clear();
        }

        private int GetOpenPermissionSlot()
        {
            int startingSlot = 0;

            while (_housePermissions.ContainsKey(startingSlot))
                startingSlot++;

            return startingSlot;
        }

        public override void InitializePermissionLevels()
        {
            for (int i = HousingConstants.MinPermissionLevel; i < HousingConstants.MaxPermissionLevel + 1; i++)
            {
                if (PermissionLevels.TryGetValue(i, out DbHousePermissions housePermissions))
                    GameServer.Database.DeleteObject(housePermissions);

                DbHousePermissions permission = new(HouseNumber, i);
                _permissionLevels[i] = permission;
                GameServer.Database.AddObject(permission);
            }
        }

        public override bool AddPermission(GamePlayer player, PermissionType permType, int permLevel)
        {
            if (player == null)
                return false;

            string targetName = permType == PermissionType.Account ? player.Client.Account.Name : player.Name;

            foreach (DbHouseCharsXPerms perm in _housePermissions.Values)
            {
                if ((PermissionType) perm.PermissionType == permType && perm.TargetName == targetName)
                    return false;
            }

            DbHouseCharsXPerms housePermission = new(HouseNumber, targetName, player.Name, permLevel, (int) permType);
            GameServer.Database.AddObject(housePermission);

            _housePermissions.TryAdd(GetOpenPermissionSlot(), housePermission);
            return true;
        }

        public override bool AddPermission(string targetName, PermissionType permType, int permLevel)
        {
            foreach (DbHouseCharsXPerms perm in _housePermissions.Values)
            {
                if ((PermissionType) perm.PermissionType == permType && perm.TargetName == targetName)
                    return false;
            }

            DbHouseCharsXPerms housePermission = new(HouseNumber, targetName, targetName, permLevel, (int) permType);
            GameServer.Database.AddObject(housePermission);

            _housePermissions.TryAdd(GetOpenPermissionSlot(), housePermission);
            return true;
        }

        public override void RemovePermission(int slot)
        {
            if (!_housePermissions.TryGetValue(slot, out DbHouseCharsXPerms matchedPerm))
                return;

            _housePermissions.TryRemove(slot, out _);
            GameServer.Database.DeleteObject(matchedPerm);
        }

        public override void ClearAndDeletePermissions()
        {
            IList<DbHousePermissions> permissions = DOLDB<DbHousePermissions>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
            GameServer.Database.DeleteObject(permissions);

            _permissionLevels.Clear();

            IList<DbHouseCharsXPerms> charPermissions = DOLDB<DbHouseCharsXPerms>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
            GameServer.Database.DeleteObject(charPermissions);

            _housePermissions.Clear();
        }

        public override void AdjustPermissionSlot(int slot, int newPermLevel)
        {
            if (!_housePermissions.TryGetValue(slot, out DbHouseCharsXPerms permission))
                return;

            if (newPermLevel is < HousingConstants.MinPermissionLevel or > HousingConstants.MaxPermissionLevel)
                return;

            permission.PermissionLevel = newPermLevel;
            GameServer.Database.SaveObject(permission);
        }

        public override void SaveIntoDatabase()
        {
            GameServer.Database.SaveObject(_databaseItem);
        }

        public override void LoadFromDatabase()
        {
            int i = 0;
            _indoorItems.Clear();

            foreach (DbHouseIndoorItem item in DOLDB<DbHouseIndoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
                AddIndoorItem(i++, new(item));

            i = 0;
            _outdoorItems.Clear();

            foreach (DbHouseOutdoorItem item in DOLDB<DbHouseOutdoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
                AddOutdoorItem(i++, new(item));

            _housePermissions.Clear();

            foreach (DbHouseCharsXPerms perm in DOLDB<DbHouseCharsXPerms>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
                _housePermissions.TryAdd(GetOpenPermissionSlot(), perm);

            _permissionLevels.Clear();

            foreach (DbHousePermissions perm in DOLDB<DbHousePermissions>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
            {
                if (!_permissionLevels.ContainsKey(perm.PermissionLevel))
                    _permissionLevels.TryAdd(perm.PermissionLevel, perm);
                else if (log.IsErrorEnabled)
                    log.ErrorFormat($"Duplicate permission level {perm.PermissionLevel} for house {HouseNumber}");
            }

            _housePointItems.Clear();

            foreach (DbHouseHookPointItem item in DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
            {
                if (!_housePointItems.ContainsKey(item.HookpointID))
                {
                    _housePointItems.TryAdd(item.HookpointID, item);
                    FillHookPoint(item.HookpointID, item.ItemTemplateID, item.Heading, item.Index);
                }
                else if (log.IsErrorEnabled)
                    log.ErrorFormat($"Duplicate item {item.ItemTemplateID} attached to hook point {item.HookpointID} for house {HouseNumber}!");
            }
        }

        public override bool AddPorch()
        {
            if (Porch)
                return false;

            Porch = true;
            SendUpdate();
            SaveIntoDatabase();
            return true;
        }

        public override bool RemovePorch()
        {
            if (!Porch)
                return false;

            RemoveConsignmentMerchant();
            Porch = false;
            SendUpdate();
            SaveIntoDatabase();
            return true;
        }

        public override bool AddConsignment(long startValue)
        {
            if (ConsignmentMerchant != null)
                return false;

            DbHouseConsignmentMerchant houseCm = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));

            if (houseCm != null)
                return false;

            DbMob obj = DOLDB<DbMob>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));

            if (obj != null)
                GameServer.Database.DeleteObject(obj);

            houseCm = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("OwnerID").IsEqualTo(OwnerID));

            if (houseCm != null)
            {
                houseCm.HouseNumber = HouseNumber;
                GameServer.Database.SaveObject(houseCm);
            }
            else
            {
                houseCm = new()
                {
                    OwnerID = OwnerID,
                    HouseNumber = HouseNumber,
                    Money = startValue
                };

                GameServer.Database.AddObject(houseCm);
            }

            float[] consignmentCoords = HousingConstants.ConsignmentPositioning[Model];
            double multi = consignmentCoords[0];
            float range = consignmentCoords[1];
            float zAdd = consignmentCoords[2];
            float realm = consignmentCoords[3];

            double angle = Heading * (Math.PI * 2 / 360);
            ushort heading = (ushort) ((Heading < 180 ? Heading + 180 : Heading - 180) / 0.08789);
            int tX = (int) (X + 500 * Math.Sin(angle) - Math.Sin(angle - multi) * range);
            int tY = (int) (Y - 500 * Math.Cos(angle) + Math.Cos(angle - multi) * range);

            GameConsignmentMerchant consignmentMerchant = GameServer.ServerRules.CreateHousingConsignmentMerchant(this);

            consignmentMerchant.CurrentRegionID = RegionID;
            consignmentMerchant.X = tX;
            consignmentMerchant.Y = tY;
            consignmentMerchant.Z = (int) (Z + zAdd);
            consignmentMerchant.Level = 50;
            consignmentMerchant.Realm = (eRealm) realm;
            consignmentMerchant.HouseNumber = (ushort) HouseNumber;
            consignmentMerchant.Heading = heading;
            consignmentMerchant.Model = 144;
            consignmentMerchant.Flags |= GameNPC.eFlags.PEACE;
            consignmentMerchant.LoadedFromScript = false;
            consignmentMerchant.RoamingRange = 0;

            if (DatabaseItem.GuildHouse)
                consignmentMerchant.GuildName = DatabaseItem.GuildName;

            consignmentMerchant.AddToWorld();
            consignmentMerchant.UpdateItems();
            consignmentMerchant.SaveIntoDatabase();

            DatabaseItem.HasConsignment = true;
            SaveIntoDatabase();

            return true;
        }

        public override bool RemoveConsignmentMerchant()
        {
            if (ConsignmentMerchant == null)
                return false;

            foreach (DbInventoryItem item in ConsignmentMerchant.GetDbItems())
            {
                item.OwnerLot = 0;
                GameServer.Database.SaveObject(item);
            }

            DbHouseConsignmentMerchant houseCM = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));

            if (houseCM != null)
            {
                houseCM.HouseNumber = 0;
                GameServer.Database.SaveObject(houseCM);
            }

            ConsignmentMerchant.DeleteFromDatabase();
            ConsignmentMerchant.Delete();
            ConsignmentMerchant = null;
            DatabaseItem.HasConsignment = false;

            SaveIntoDatabase();
            return true;
        }

        public override void PickUpConsignmentMerchant(GamePlayer player)
        {
            if (!CanEmptyHookPoint(player))
            {
                ChatUtil.SendSystemMessage(player, "You don't have the permission to remove this consignment merchant.");
                return;
            }

            DbItemTemplate itemTemplate = GameServer.Database.FindObjectByKey<DbItemTemplate>("housing_consignment_deed");

            if (itemTemplate == null || !RemoveConsignmentMerchant())
            {
                ChatUtil.SendSystemMessage(player, "Couldn't pick up the Consignment Merchant due to an internal error.");
                return;
            }

            if (!player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(itemTemplate)))
            {
                ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.PickupObject.BackpackFull"));
                return;
            }

            InventoryLogging.LogInventoryAction($"(HOUSE;{HouseNumber})", player, eInventoryActionType.Loot, itemTemplate);
        }

        public override void Edit(GamePlayer player, List<int> changes)
        {
            MerchantTradeItems items = player.InHouse ? HouseTemplateMgr.IndoorMenuItems : HouseTemplateMgr.OutdoorMenuItems;

            if (items == null)
                return;

            if (!player.InHouse && !CanChangeExternalAppearance(player))
                return;

            if (player.InHouse && !CanChangeInterior(player, DecorationPermissions.Add))
                return;

            long price = 0;

            foreach (int slot in changes)
            {
                int page = slot / 30;
                int pSlot = slot % 30;

                DbItemTemplate item = items.GetItem(page, (eMerchantWindowSlot) pSlot);

                if (item != null)
                    price += item.Price;
            }

            if (!player.RemoveMoney(price))
            {
                InventoryLogging.LogInventoryAction(player, $"(HOUSE;{HouseNumber})", eInventoryActionType.Merchant, price);
                ChatUtil.SendMerchantMessage(player, "House.Edit.NotEnoughMoney", null);
                return;
            }

            ChatUtil.SendSystemMessage(player, "House.Edit.PayForChanges", Money.GetString(price));

            foreach (int slot in changes)
            {
                int page = slot / 30;
                int pSlot = slot % 30;
                DbItemTemplate item = items.GetItem(page, (eMerchantWindowSlot) pSlot);

                if (item != null)
                {
                    switch ((eObjectType) item.Object_Type)
                    {
                        case eObjectType.HouseInteriorBanner:
                        {
                            IndoorGuildBanner = item.DPS_AF == 1;
                            break;
                        }
                        case eObjectType.HouseInteriorShield:
                        {
                            IndoorGuildShield = item.DPS_AF == 1;
                            break;
                        }
                        case eObjectType.HouseCarpetFirst:
                        {
                            Rug1Color = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseCarpetSecond:
                        {
                            Rug2Color = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseCarpetThird:
                        {
                            Rug3Color = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseCarpetFourth:
                        {
                            Rug4Color = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseTentColor:
                        {
                            PorchRoofColor = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseExteriorBanner:
                        {
                            OutdoorGuildBanner = item.DPS_AF == 1;
                            break;
                        }
                        case eObjectType.HouseExteriorShield:
                        {
                            OutdoorGuildShield = item.DPS_AF == 1;
                            break;
                        }
                        case eObjectType.HouseRoofMaterial:
                        {
                            RoofMaterial = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseWallMaterial:
                        {
                            WallMaterial = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseDoorMaterial:
                        {
                            DoorMaterial = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HousePorchMaterial:
                        {
                            PorchMaterial = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseWoodMaterial:
                        {
                            TrussMaterial = item.DPS_AF;
                            break;
                        }
                        case eObjectType.HouseShutterMaterial:
                        {
                            WindowMaterial = item.DPS_AF;
                            break;
                        }
                    }
                }
            }

            SaveIntoDatabase();
            SendUpdate();
        }

        public override bool FillHookPoint(uint position, string templateID, ushort heading, int index)
        {
            DbItemTemplate item = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateID);

            if (item == null)
                return false;

            Point3D location = GetHookPointLocation(position);

            if (location == null)
                return false;

            GameObject hookPointObject = null;

            switch ((eObjectType) item.Object_Type)
            {
                case eObjectType.HouseVault:
                {
                    GameHouseVault houseVault = new(item, index);
                    houseVault.Attach(this, position);
                    hookPointObject = houseVault;
                    _houseVaults[index] = houseVault;
                    break;
                }
                case eObjectType.HouseNPC:
                {
                    hookPointObject = GameServer.ServerRules.PlaceHousingNPC(this, item, location, GetHookPointHeading(position));
                    break;
                }
                case eObjectType.HouseBindstone:
                {
                    hookPointObject = new GameStaticItem
                    {
                        CurrentHouse = this,
                        InHouse = true,
                        OwnerID = templateID,
                        X = location.X,
                        Y = location.Y,
                        Z = location.Z,
                        Heading = GetHookPointHeading(position),
                        CurrentRegionID = RegionID,
                        Name = item.Name,
                        Model = (ushort)item.Model
                    };
                    hookPointObject.AddToWorld();
                    break;
                }
                case eObjectType.HouseInteriorObject:
                {
                    hookPointObject = GameServer.ServerRules.PlaceHousingInteriorItem(this, item, location, heading);
                    break;
                }
            }

            if (hookPointObject != null)
            {
                if (HousePointItems.TryGetValue(position, out DbHouseHookPointItem hpItem))
                    hpItem.GameObject = hookPointObject;

                return true;
            }

            return false;
        }

        public override void EmptyHookPoint(GamePlayer player, GameObject obj, bool addToInventory = true)
        {
            if (player.CurrentHouse != this || !CanEmptyHookPoint(player))
            {
                ChatUtil.SendSystemMessage(player, "Only the Owner of a House can remove or place Items on hook points!");
                return;
            }

            int position = GetHookPointPosition(obj.X, obj.Y);

            if (position < 0)
            {
                ChatUtil.SendSystemMessage(player, $"Invalid hook point position {position}");
                return;
            }

            var items = DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HookpointID").IsEqualTo(position).And(DB.Column("HouseNumber").IsEqualTo(obj.CurrentHouse.HouseNumber)));

            if (items.Count == 0)
            {
                ChatUtil.SendSystemMessage(player, $"No hook point item found at position {position}");
                return;
            }

            GameServer.Database.DeleteObject(items);
            obj.Delete();

            RemoveHousePointItem((uint)position);

            if (obj is GameHouseVault vault)
                RemoveHouseVault(vault.Index);

            SendUpdate();

            if (addToInventory)
            {
                DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(obj.OwnerID);

                if (template != null)
                {
                    if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(template)))
                        InventoryLogging.LogInventoryAction($"(HOUSE;{HouseNumber})", player, eInventoryActionType.Loot, template);
                }
            }
        }

        public override int GetAvailableVaultSlot()
        {
            bool[] slots = new bool[MAX_VAULT_COUNT];

            foreach (DbHouseHookPointItem housePointItem in DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber).And(DB.Column("ItemTemplateID").IsLike("%_vault"))))
            {
                if (housePointItem.Index is >= 0 and < MAX_VAULT_COUNT)
                    slots[housePointItem.Index] = true;
            }

            for (int availableSlot = 0; availableSlot < MAX_VAULT_COUNT; availableSlot++)
            {
                if (!slots[availableSlot])
                    return availableSlot;
            }

            return -1;
        }
    }

    public class NullHouse : House
    {
        private static readonly ReadOnlyDictionary<int, IndoorItem> _emptyIndoor = new(new Dictionary<int, IndoorItem>());
        private static readonly ReadOnlyDictionary<int, OutdoorItem> _emptyOutdoor = new(new Dictionary<int, OutdoorItem>());
        private static readonly ReadOnlyDictionary<uint, DbHouseHookPointItem> _emptyHousePoint = new(new Dictionary<uint, DbHouseHookPointItem>());
        private static readonly ReadOnlyDictionary<int, GameHouseVault> _emptyVault = new(new Dictionary<int, GameHouseVault>());
        private static readonly ReadOnlyDictionary<int, DbHouseCharsXPerms> _emptyPerms = new(new Dictionary<int, DbHouseCharsXPerms>());
        private static readonly ReadOnlyDictionary<int, DbHousePermissions> _emptyPermLevels = new(new Dictionary<int, DbHousePermissions>());

        public override IReadOnlyDictionary<int, IndoorItem> IndoorItems => _emptyIndoor;
        public override IReadOnlyDictionary<int, OutdoorItem> OutdoorItems => _emptyOutdoor;
        public override IReadOnlyDictionary<uint, DbHouseHookPointItem> HousePointItems => _emptyHousePoint;
        public override IReadOnlyDictionary<int, GameHouseVault> HouseVaults => _emptyVault;
        public override IReadOnlyDictionary<int, DbHouseCharsXPerms> CharXPermissions => _emptyPerms;
        public override IEnumerable<KeyValuePair<int, DbHouseCharsXPerms>> HousePermissions => [];
        public override IReadOnlyDictionary<int, DbHousePermissions> PermissionLevels => _emptyPermLevels;

        public NullHouse(string ownerId, bool guildHouse) : base(new()
        {
            AllowAdd = false,
            AllowDelete = false,
            OwnerID = ownerId,
            GuildHouse = guildHouse
        }) { }

        public override bool AddIndoorItem(int key, IndoorItem item)
        {
            return false;
        }

        public override bool RemoveIndoorItem(int key)
        {
            return false;
        }

        public override void ClearAndDeleteIndoorItems() { }

        public override bool AddOutdoorItem(int key, OutdoorItem item)
        {
            return false;
        }

        public override bool RemoveOutdoorItem(int key)
        {
            return false;
        }

        public override void ClearAndDeleteOutdoorItems() { }

        public override bool AddHousePointItem(uint key, DbHouseHookPointItem item)
        {
            return false;
        }

        public override bool RemoveHousePointItem(uint key)
        {
            return false;
        }

        public override void ClearAndDeleteHousePointItems() { }

        public override bool AddHouseVault(int key, GameHouseVault vault)
        {
            return false;
        }

        public override bool RemoveHouseVault(int key)
        {
            return false;
        }

        public override void ClearHouseVaults() { }

        public override void InitializePermissionLevels() { }

        public override bool AddPermission(GamePlayer player, PermissionType permType, int permLevel)
        {
            return false;
        }

        public override bool AddPermission(string targetName, PermissionType permType, int permLevel)
        {
            return false;
        }

        public override void RemovePermission(int slot) { }

        public override void ClearAndDeletePermissions() { }

        public override void AdjustPermissionSlot(int slot, int newPermLevel) { }

        public override void SaveIntoDatabase() { }

        public override void LoadFromDatabase() { }

        public override bool AddPorch()
        {
            return false;
        }

        public override bool RemovePorch()
        {
            return false;
        }

        public override bool AddConsignment(long startValue)
        {
            return false;
        }

        public override bool RemoveConsignmentMerchant()
        {
            return false;
        }

        public override void PickUpConsignmentMerchant(GamePlayer player) { }

        public override void Edit(GamePlayer player, List<int> changes) { }

        public override bool FillHookPoint(uint position, string templateID, ushort heading, int index)
        {
            return false;
        }

        public override void EmptyHookPoint(GamePlayer player, GameObject obj, bool addToInventory = true) { }

        public override int GetAvailableVaultSlot()
        {
            return -1;
        }
    }
}
