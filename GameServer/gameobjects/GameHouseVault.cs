using System;
using System.Collections;
using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS
{
	/// <summary>
	/// A house vault.
	/// </summary>
	/// <author>Aredhel</author>
	public class GameHouseVault : GameVault, IHouseHookpointItem
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly string _templateID;
		private readonly object _vaultLock = new object();
		private DbHouseHookPointItem _hookedItem;

		/// <summary>
		/// Create a new house vault.
		/// </summary>
		/// <param name="vaultIndex"></param>
		public GameHouseVault(DbItemTemplate itemTemplate, int vaultIndex)
		{
			if (itemTemplate == null)
				throw new ArgumentNullException();

			Name = itemTemplate.Name;
			Model = (ushort) (itemTemplate.Model);
			_templateID = itemTemplate.Id_nb;
			Index = vaultIndex;
		}

		public override int VaultSize
		{
			get { return HousingConstants.VaultSize; }
		}

		#region IHouseHookpointItem Members

		/// <summary>
		/// Template ID for this vault.
		/// </summary>
		public string TemplateID
		{
			get { return _templateID; }
		}

        /// <summary>
        /// Attach this vault to a hookpoint in a house.
        /// </summary>
        /// <param name="house"></param>
        /// <param name="hookpointID"></param>
        /// <returns></returns>
        public bool Attach(House house, uint hookpointID, ushort heading)
        {
            if (house == null)
                return false;

            // register vault in the DB.
            var hookedItem = new DbHouseHookPointItem
            {
                HouseNumber = house.HouseNumber,
                HookpointID = hookpointID,
                Heading = (ushort)(heading % 4096),
                ItemTemplateID = _templateID,
                Index = (byte)Index
            };

            var hpitem = DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(house.HouseNumber).And(DB.Column("HookpointID").IsEqualTo(hookpointID)));

			// if there isn't anything already on this hookpoint then add it to the DB
			if (hpitem.Count == 0)
			{
				GameServer.Database.AddObject(hookedItem);
			}

            // now add the vault to the house.
            return Attach(house, hookedItem);
        }

		/// <summary>
		/// Attach this vault to a hookpoint in a house.
		/// </summary>
		/// <param name="house"></param>
		/// <param name="hookedItem"></param>
		/// <returns></returns>
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
			Heading = (ushort) (hookedItem.Heading%4096);
			AddToWorld();

			return true;
		}

		/// <summary>
		/// Remove this vault from a hookpoint in the house.
		/// </summary>
		/// <returns></returns>
		public bool Detach(GamePlayer player)
		{
			if (_hookedItem == null || CurrentHouse != player.CurrentHouse || CurrentHouse.CanEmptyHookpoint(player) == false)
				return false;

			lock (m_vaultSync)
			{
				foreach (GamePlayer observer in _observers.Values)
				{
					observer.ActiveInventoryObject = null;
				}

				_observers.Clear();
				_hookedItem = null;

				CurrentHouse.EmptyHookpoint(player, this, false);
			}

			return true;
		}

		#endregion


		public override string GetOwner(GamePlayer player = null)
		{
			return CurrentHouse.DatabaseItem.OwnerID;
		}


		public override IList GetExamineMessages(GamePlayer player)
		{
			IList list = new ArrayList();
			list.Add("[Right click to display the contents of house vault " + (m_vaultIndex + 1) + "]");
			return list;
		}

		public override string Name
		{
			get
			{
				return base.Name + " " + (m_vaultIndex + 1);
			}
			set
			{
				base.Name = value;
			}
		}

		/// <summary>
		/// Player interacting with this vault.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool Interact(GamePlayer player)
		{
			if (!player.InHouse)
				return false;

			if (!base.Interact(player) || CurrentHouse == null || CurrentHouse != player.CurrentHouse)
				return false;

			lock (_vaultLock)
			{
				if (!_observers.ContainsKey(player.Name))
				{
					_observers.Add(player.Name, player);
				}
			}

			return true;
		}

		/// <summary>
		/// Whether or not this player can view the contents of this
		/// vault.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool CanView(GamePlayer player)
		{

			if (player.HCFlag || player.NoHelp)
			{
				return false;
			}
			
			return CurrentHouse.CanUseVault(player, this, VaultPermissions.View);
		}

		/// <summary>
		/// Whether or not this player can move items inside the vault
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool CanAddItems(GamePlayer player)
		{
			if (player.HCFlag || player.NoHelp)
			{
				return false;
			}
			
			return CurrentHouse.CanUseVault(player, this, VaultPermissions.Add);
		}

		/// <summary>
		/// Whether or not this player can move items inside the vault
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool CanRemoveItems(GamePlayer player)
		{
			if (player.HCFlag)
			{
				return false;
			}
			
			return CurrentHouse.CanUseVault(player, this, VaultPermissions.Remove);
		}
	}
}