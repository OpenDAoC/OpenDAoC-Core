using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS
{
    /// <summary>
    /// A house vault.
    /// </summary>
    public class GameHouseVault : GameVault, IHouseHookpointItem
    {
        private readonly object _vaultLock = new();
        private DbHouseHookPointItem _hookedItem;

        /// <summary>
        /// Create a new house vault.
        /// </summary>
        public GameHouseVault(DbItemTemplate itemTemplate, int vaultIndex)
        {
            ArgumentNullException.ThrowIfNull(itemTemplate);
            Name = itemTemplate.Name;
            Model = (ushort) itemTemplate.Model;
            TemplateID = itemTemplate.Id_nb;
            Index = vaultIndex;
        }

        public override int VaultSize => HousingConstants.VaultSize;

        /// <summary>
        /// Template ID for this vault.
        /// </summary>
        public string TemplateID { get; }

        /// <summary>
        /// Attach this vault to a hook point in a house.
        /// </summary>
        public bool Attach(House house, uint hookPointId, ushort heading)
        {
            if (house == null)
                return false;

            DbHouseHookPointItem hookedItem = new()
            {
                HouseNumber = house.HouseNumber,
                HookpointID = hookPointId,
                Heading = (ushort)(heading % 4096),
                ItemTemplateID = TemplateID,
                Index = (byte)Index
            };

            IList<DbHouseHookPointItem> hookPoints = DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber).And(DB.Column("HookpointID").IsEqualTo(hookPointId)));

            if (hookPoints.Count == 0)
                GameServer.Database.AddObject(hookedItem);

            return Attach(house, hookedItem);
        }

        /// <summary>
        /// Attach this vault to a hook point in a house.
        /// </summary>
        public bool Attach(House house, DbHouseHookPointItem hookedItem)
        {
            if (house == null || hookedItem == null)
                return false;

            _hookedItem = hookedItem;

            IPoint3D position = house.GetHookpointLocation(hookedItem.HookpointID);

            if (position == null)
                return false;

            CurrentHouse = house;
            CurrentRegionID = house.RegionID;
            InHouse = true;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            Heading = (ushort) (hookedItem.Heading % 4096);
            AddToWorld();
            return true;
        }

        /// <summary>
        /// Remove this vault from a hook point in the house.
        /// </summary>
        public bool Detach(GamePlayer player)
        {
            if (_hookedItem == null || CurrentHouse != player.CurrentHouse || CurrentHouse.CanEmptyHookpoint(player) == false)
                return false;

            lock (LockObject)
            {
                foreach (GamePlayer observer in _observers.Values)
                    observer.ActiveInventoryObject = null;

                _observers.Clear();
                _hookedItem = null;

                CurrentHouse.EmptyHookpoint(player, this, false);
            }

            return true;
        }

        public override string GetOwner(GamePlayer player = null)
        {
            return CurrentHouse.DatabaseItem.OwnerID;
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            List<string> list = [$"[Right click to display the contents of house vault {Index + 1}]"];
            return list;
        }

        public override string Name
        {
            get => $"base.Name {Index + 1}";
            set => base.Name = value;
        }

        /// <summary>
        /// Player interacting with this vault.
        /// </summary>
        public override bool Interact(GamePlayer player)
        {
            if (!player.InHouse || !base.Interact(player) || CurrentHouse == null || CurrentHouse != player.CurrentHouse)
                return false;

            lock (_vaultLock)
            {
                _observers.TryAdd(player.Name, player);
            }

            return true;
        }

        /// <summary>
        /// Whether or not this player can view the contents of this vault.
        /// </summary>
        public override bool CanView(GamePlayer player)
        {
            return !player.HCFlag && !player.NoHelp && CurrentHouse.CanUseVault(player, this, VaultPermissions.View);
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault
        /// </summary>
        public override bool CanAddItems(GamePlayer player)
        {
            return !player.HCFlag && !player.NoHelp && CurrentHouse.CanUseVault(player, this, VaultPermissions.Add);
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault
        /// </summary>
        public override bool CanRemoveItems(GamePlayer player)
        {
            return !player.HCFlag && CurrentHouse.CanUseVault(player, this, VaultPermissions.Remove);
        }
    }
}
