using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Language;

namespace DOL.GS.Housing
{
	public class House : Point3D, IGameLocation
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		private const int MAX_VAULT_COUNT = 8; // Must not be bigger than what `eInventorySlot` allows.

		private readonly DbHouse _databaseItem;
		private readonly Dictionary<int, DbHouseCharsXPerms> _housePermissions;
		private readonly Dictionary<uint, DbHouseHookPointItem> _housepointItems;
		private readonly Dictionary<int, IndoorItem> _indoorItems;
		private readonly Dictionary<int, OutdoorItem> _outdoorItems;
		private readonly Dictionary<int, DbHousePermissions> _permissionLevels;
		private GameConsignmentMerchant _consignmentMerchant;

		#region Properties

		public int HouseNumber
		{
			get { return _databaseItem.HouseNumber; }
			set { _databaseItem.HouseNumber = value; }
		}

		public string OwnerID
		{
			get { return _databaseItem.OwnerID; }
			set { _databaseItem.OwnerID = value; }
		}

		public int Model
		{
			get { return _databaseItem.Model; }
			set { _databaseItem.Model = value; }
		}

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

		public int Emblem
		{
			get { return _databaseItem.Emblem; }
			set { _databaseItem.Emblem = value; }
		}

		public int PorchRoofColor
		{
			get { return _databaseItem.PorchRoofColor; }
			set { _databaseItem.PorchRoofColor = value; }
		}

		public int PorchMaterial
		{
			get { return _databaseItem.PorchMaterial; }
			set { _databaseItem.PorchMaterial = value; }
		}

		public bool Porch
		{
			get { return _databaseItem.Porch; }
			set { _databaseItem.Porch = value; }
		}

		public bool IndoorGuildBanner
		{
			get { return _databaseItem.IndoorGuildBanner; }
			set { _databaseItem.IndoorGuildBanner = value; }
		}

		public bool IndoorGuildShield
		{
			get { return _databaseItem.IndoorGuildShield; }
			set { _databaseItem.IndoorGuildShield = value; }
		}

		public bool OutdoorGuildBanner
		{
			get { return _databaseItem.OutdoorGuildBanner; }
			set { _databaseItem.OutdoorGuildBanner = value; }
		}

		public bool OutdoorGuildShield
		{
			get { return _databaseItem.OutdoorGuildShield; }
			set { _databaseItem.OutdoorGuildShield = value; }
		}

		public int RoofMaterial
		{
			get { return _databaseItem.RoofMaterial; }
			set { _databaseItem.RoofMaterial = value; }
		}

		public int DoorMaterial
		{
			get { return _databaseItem.DoorMaterial; }
			set { _databaseItem.DoorMaterial = value; }
		}

		public int WallMaterial
		{
			get { return _databaseItem.WallMaterial; }
			set { _databaseItem.WallMaterial = value; }
		}

		public int TrussMaterial
		{
			get { return _databaseItem.TrussMaterial; }
			set { _databaseItem.TrussMaterial = value; }
		}

		public int WindowMaterial
		{
			get { return _databaseItem.WindowMaterial; }
			set { _databaseItem.WindowMaterial = value; }
		}

		public int Rug1Color
		{
			get { return _databaseItem.Rug1Color; }
			set { _databaseItem.Rug1Color = value; }
		}

		public int Rug2Color
		{
			get { return _databaseItem.Rug2Color; }
			set { _databaseItem.Rug2Color = value; }
		}

		public int Rug3Color
		{
			get { return _databaseItem.Rug3Color; }
			set { _databaseItem.Rug3Color = value; }
		}

		public int Rug4Color
		{
			get { return _databaseItem.Rug4Color; }
			set { _databaseItem.Rug4Color = value; }
		}

		public DateTime LastPaid
		{
			get { return _databaseItem.LastPaid; }
			set { _databaseItem.LastPaid = value; }
		}

		public long KeptMoney
		{
			get { return _databaseItem.KeptMoney; }
			set { _databaseItem.KeptMoney = value; }
		}

		public bool NoPurge
		{
			get { return _databaseItem.NoPurge; }
			set { _databaseItem.NoPurge = value; }
		}

		public int UniqueID { get; set; }

		public IDictionary<int, IndoorItem> IndoorItems
		{
			get { return _indoorItems; }
		}

		public IDictionary<int, OutdoorItem> OutdoorItems
		{
			get { return _outdoorItems; }
		}

		public IDictionary<uint, DbHouseHookPointItem> HousepointItems
		{
			get { return _housepointItems; }
		}

		public DbHouse DatabaseItem
		{
			get { return _databaseItem; }
		}

		public IEnumerable<KeyValuePair<int, DbHouseCharsXPerms>> HousePermissions
		{
			get { return _housePermissions.OrderBy(entry => entry.Value.CreationTime); }
		}

		public IDictionary<int, DbHouseCharsXPerms> CharXPermissions
		{
			get { return _housePermissions; }
		}

		public IDictionary<int, DbHousePermissions> PermissionLevels
		{
			get { return _permissionLevels; }
		}

		public GameConsignmentMerchant ConsignmentMerchant
		{
			get { return _consignmentMerchant; }
			set { _consignmentMerchant = value; }
		}

		public bool IsOccupied
		{
			get
			{
				foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(RegionID, X, Y, 25000, WorldMgr.VISIBILITY_DISTANCE))
				{
					if (player.CurrentHouse == this && player.InHouse)
					{
						return true;
					}
				}

				return false;
			}
		}

		public override int X
		{
			get { return _databaseItem.X; }
			set { _databaseItem.X = value; }
		}

		public override int Y
		{
			get { return _databaseItem.Y; }
			set { _databaseItem.Y = value; }
		}

		public override int Z
		{
			get { return _databaseItem.Z; }
			set { _databaseItem.Z = value; }
		}

		public ushort RegionID
		{
			get { return _databaseItem.RegionID; }
			set { _databaseItem.RegionID = value; }
		}

		public ushort Heading
		{
			get { return (ushort) _databaseItem.Heading; }
			set { _databaseItem.Heading = value; }
		}

		public string Name
		{
			get { return _databaseItem.Name; }
			set { _databaseItem.Name = value; }
		}

		#endregion

		public House(DbHouse house)
		{
			_databaseItem = house;
			_permissionLevels = new Dictionary<int, DbHousePermissions>();
			_indoorItems = new Dictionary<int, IndoorItem>();
			_outdoorItems = new Dictionary<int, OutdoorItem>();
			_housepointItems = new Dictionary<uint, DbHouseHookPointItem>();
			_housePermissions = new Dictionary<int, DbHouseCharsXPerms>();
		}

		/// <summary>
		/// The spot you are teleported to when you exit this house.
		/// </summary>
		public GameLocation OutdoorJumpPoint
		{
			get
			{
				double angle = Heading*((Math.PI*2)/360); // angle*2pi/360;
				var x = (int) (X + (0*Math.Cos(angle) + 500*Math.Sin(angle)));
				var y = (int) (Y - (500*Math.Cos(angle) - 0*Math.Sin(angle)));
				var heading = (ushort) ((Heading < 180 ? Heading + 180 : Heading - 180)/0.08789);

				return new GameLocation("Housing", RegionID, x, y, Z, heading);
			}
		}

		/// <summary>
		/// Sends a update of the house and the garden to all players in range
		/// </summary>
		public void SendUpdate()
		{
			foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(this, HousingConstants.HouseViewingDistance))
			{
				player.Out.SendHouse(this);
				player.Out.SendGarden(this);
			}
			
			foreach (GamePlayer player in GetAllPlayersInHouse())
			{
				player.Out.SendEnterHouse(this);
			}
		}

		/// <summary>
		/// Used to get into a house
		/// </summary>
		/// <param name="player">the player who wants to get in</param>
		public void Enter(GamePlayer player)
		{
			IList<GamePlayer> list = GetAllPlayersInHouse();
			if (list.Count == 0)
			{
				foreach (GamePlayer pl in WorldMgr.GetPlayersCloseToSpot(this, HousingConstants.HouseViewingDistance))
				{
					pl.Out.SendHouseOccupied(this, true);
				}
			}

			ChatUtil.SendSystemMessage(player, "House.Enter.EnteringHouse", HouseNumber);
			player.Out.SendEnterHouse(this);
			player.Out.SendFurniture(this);

			player.InHouse = true;
			player.CurrentHouse = this;

			ushort heading = 0;

			switch (Model)
			{
					//thx to sp4m
				default:
					player.MoveTo(RegionID, X, Y, 25022, heading);
					break;

				case 1:
					player.MoveTo(RegionID, X + 80, Y + 100, ((25025)), heading);
					break;

				case 2:
					player.MoveTo(RegionID, X - 260, Y + 100, ((24910)), heading);
					break;

				case 3:
					player.MoveTo(RegionID, X - 200, Y + 100, ((24800)), heading);
					break;

				case 4:
					player.MoveTo(RegionID, X - 350, Y - 30, ((24660)), heading);
					break;

				case 5:
					player.MoveTo(RegionID, X + 230, Y - 480, ((25100)), heading);
					break;

				case 6:
					player.MoveTo(RegionID, X - 80, Y - 660, ((24700)), heading);
					break;

				case 7:
					player.MoveTo(RegionID, X - 80, Y - 660, ((24700)), heading);
					break;

				case 8:
					player.MoveTo(RegionID, X - 90, Y - 625, ((24670)), heading);
					break;

				case 9:
					player.MoveTo(RegionID, X + 400, Y - 160, ((25150)), heading);
					break;

				case 10:
					player.MoveTo(RegionID, X + 400, Y - 80, ((25060)), heading);
					break;

				case 11:
					player.MoveTo(RegionID, X + 400, Y - 60, ((24900)), heading);
					break;

				case 12:
					player.MoveTo(RegionID, X, Y - 620, ((24595)), heading);
					break;
			}

			ChatUtil.SendSystemMessage(player, "House.Enter.EnteredHouse", HouseNumber);
		}

		/// <summary>
		/// Used to leave a house
		/// </summary>
		/// <param name="player">the player who wants to get in</param>
		/// <param name="silent">text or not</param>
		public void Exit(GamePlayer player, bool silent)
		{
			player.MoveTo(OutdoorJumpPoint);

			if (!silent)
			{
				ChatUtil.SendSystemMessage(player, "House.Exit.LeftHouse", HouseNumber);
			}

			player.Out.SendExitHouse(this);

			IList<GamePlayer> list = GetAllPlayersInHouse();
			if (list.Count == 0)
			{
				foreach (GamePlayer pl in WorldMgr.GetPlayersCloseToSpot(this, HousingConstants.HouseViewingDistance))
				{
					pl.Out.SendHouseOccupied(this, false);
				}
			}
		}

		/// <summary>
		/// Sends the house info window to a player
		/// </summary>
		/// <param name="player">the player</param>
		public void SendHouseInfo(GamePlayer player)
		{
			int level = Model - ((Model - 1) / 4) * 4;
			TimeSpan due = (LastPaid.AddDays(ServerProperties.Properties.RENT_DUE_DAYS).AddHours(1) - DateTime.Now);
			var text = new List<string>();

			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Owner", Name));
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Lotnum", HouseNumber));

			if (level > 0)
				text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Level", level));
			else
				text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Level", "Lot"));

			text.Add(" ");
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.Lockbox", Money.GetString(KeptMoney)));
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.RentalPrice", Money.GetString(HouseMgr.GetRentByModel(Model))));
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.MaxLockbox", Money.GetString(HouseMgr.GetRentByModel(Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS)));
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
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.PorchEnabled", (Porch ? "Y" : "N")));
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
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.OutdoorGuildBanner", ((OutdoorGuildBanner) ? "Y" : "N")));
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.OutdoorGuildShield", ((OutdoorGuildShield) ? "Y" : "N")));
			text.Add(" ");
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.InteriorUpgrades"));
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.IndoorGuildBanner", ((IndoorGuildBanner) ? "Y" : "N")));
			text.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "House.SendHouseInfo.IndoorGuildShield", ((IndoorGuildShield) ? "Y" : "N")));
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

		/// <summary>
		/// Returns a ArrayList with all players in the house
		/// </summary>
		public IList<GamePlayer> GetAllPlayersInHouse()
		{
			var ret = new List<GamePlayer>();
			foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(RegionID, X, Y, 25000, WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player.CurrentHouse == this && player.InHouse)
				{
					ret.Add(player);
				}
			}

			return ret;
		}

		public int GetAvailableVaultSlot()
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

		#region Hookpoints

		public static bool AddNewOffset(DbHouseHookPointOffset o)
		{
			if (o.HookpointID <= HousingConstants.MaxHookpointLocations)
			{
				HousingConstants.RelativeHookpointsCoords[o.HouseModel][o.HookpointID] = new[] {o.X, o.Y, o.Z, o.Heading};
				return true;
			}

			if (log.IsErrorEnabled)
				log.Error("[Housing]: HouseHookPointOffset exceeds array size.  Model " + o.HouseModel + ", hookpoint " + o.HookpointID);

			return false;
		}

		public static void LoadHookpointOffsets()
		{
			for (int i = HousingConstants.MaxHouseModel; i > 0; i--)
			{
				for (int j = 1; j < HousingConstants.RelativeHookpointsCoords[i].Length; j++)
				{
					HousingConstants.RelativeHookpointsCoords[i][j] = null;
				}
			}

			IList<DbHouseHookPointOffset> objs = GameServer.Database.SelectAllObjects<DbHouseHookPointOffset>();
			foreach (DbHouseHookPointOffset o in objs)
			{
				AddNewOffset(o);
			}
		}

		public Point3D GetHookpointLocation(uint n)
		{
			if (n > HousingConstants.MaxHookpointLocations)
				return null;

			int[] hookpointsCoords = HousingConstants.RelativeHookpointsCoords[Model][n];

			if (hookpointsCoords == null)
				return null;

			return new Point3D(X + hookpointsCoords[0], Y + hookpointsCoords[1], 25000 + hookpointsCoords[2]);
		}

		private int GetHookpointPosition(int objX, int objY, int objZ)
		{
			int position = -1;

			for (int i = 0; i < HousingConstants.MaxHookpointLocations; i++)
			{
				if (HousingConstants.RelativeHookpointsCoords[Model][i] != null)
				{
					if (HousingConstants.RelativeHookpointsCoords[Model][i][0] + X == objX &&
					    HousingConstants.RelativeHookpointsCoords[Model][i][1] + Y == objY)
					{
						position = i;
					}
				}
			}

			return position;
		}

		public ushort GetHookpointHeading(uint n)
		{
			if (n > HousingConstants.MaxHookpointLocations)
				return 0;

			int[] hookpointsCoords = HousingConstants.RelativeHookpointsCoords[Model][n];

			if (hookpointsCoords == null)
				return 0;

			return (ushort) (Heading + hookpointsCoords[3]);
		}

		/// <summary>
		/// Fill a hookpoint with an object, create it in the database.
		/// </summary>
		/// <param name="item">The itemtemplate of the item used to fill the hookpoint (can be null if templateid is filled)</param>
		/// <param name="position">The position of the hookpoint</param>
		/// <param name="templateID">The template id of the item (can be blank if item is filled)</param>
		/// <param name="heading">The requested heading of this house item</param>
		public bool FillHookpoint(uint position, string templateID, ushort heading, int index)
		{
			DbItemTemplate item = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateID);

			if (item == null)
				return false;

			//get location from slot
			IPoint3D location = GetHookpointLocation(position);
			if (location == null)
				return false;

			int x = location.X;
			int y = location.Y;
			int z = location.Z;
			GameObject hookpointObject = null;

			switch ((eObjectType)item.Object_Type)
			{
				case eObjectType.HouseVault:
				{
					var houseVault = new GameHouseVault(item, index);
					houseVault.Attach(this, position);
					hookpointObject = houseVault;
					break;
				}
				case eObjectType.HouseNPC:
				{
					hookpointObject = GameServer.ServerRules.PlaceHousingNPC(this, item, location, GetHookpointHeading(position));
					break;
				}
				case eObjectType.HouseBindstone:
				{
					hookpointObject = new GameStaticItem();
					hookpointObject.CurrentHouse = this;
					hookpointObject.InHouse = true;
					hookpointObject.OwnerID = templateID;
					hookpointObject.X = x;
					hookpointObject.Y = y;
					hookpointObject.Z = z;
					hookpointObject.Heading = GetHookpointHeading(position);
					hookpointObject.CurrentRegionID = RegionID;
					hookpointObject.Name = item.Name;
					hookpointObject.Model = (ushort) item.Model;
					hookpointObject.AddToWorld();
					//0:07:45.984 S=>C 0xD9 item/door create v171 (oid:0x0DDB emblem:0x0000 heading:0x0DE5 x:596203 y:530174 z:24723 model:0x05D2 health:  0% flags:0x04(realm:0) extraBytes:0 unk1_171:0x0096220C name:"Hibernia bindstone")
					//add bind point
					break;
				}
				case eObjectType.HouseInteriorObject:
				{
					hookpointObject = GameServer.ServerRules.PlaceHousingInteriorItem(this, item, location, heading);
					break;
				}
			}

			if (hookpointObject != null)
			{
				HousepointItems[position].GameObject = hookpointObject;
				return true;
			}

			return false;
		}

		public void EmptyHookpoint(GamePlayer player, GameObject obj, bool addToInventory = true)
		{
			if (player.CurrentHouse != this || CanEmptyHookpoint(player) == false)
			{
				ChatUtil.SendSystemMessage(player, "Only the Owner of a House can remove or place Items on Hookpoints!");
				return;
			}

			int position = GetHookpointPosition(obj.X, obj.Y, obj.Z);

			if (position < 0)
			{
				ChatUtil.SendSystemMessage(player, "Invalid hookpoint position " + position);
				return;
			}

			var items = DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HookpointID").IsEqualTo(position).And(DB.Column("HouseNumber").IsEqualTo(obj.CurrentHouse.HouseNumber)));
			if (items.Count == 0)
			{
				ChatUtil.SendSystemMessage(player, "No hookpoint item found at position " + position);
				return;
			}

			// clear every item from this hookpoint
			GameServer.Database.DeleteObject(items);

			obj.Delete();
			player.CurrentHouse.HousepointItems.Remove((uint)position);
			player.CurrentHouse.SendUpdate();

			if (addToInventory)
			{
				var template = GameServer.Database.FindObjectByKey<DbItemTemplate>(obj.OwnerID);
				if (template != null)
				{
                    if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(template)))
                        InventoryLogging.LogInventoryAction("(HOUSE;" + HouseNumber + ")", player, eInventoryActionType.Loot, template);
				}
			}
		}

		#endregion

		#region Editing

		public bool AddPorch()
		{
			if (Porch) // we can't add an already added porch
				return false;

			// set porch to true, we now have one!
			Porch = true;

			// broadcast updates
			SendUpdate();
			SaveIntoDatabase();

			return true;
		}

		public bool RemovePorch()
		{
			if (!Porch) // we can't remove an already removed porch
				return false;

			// remove the consignment merchant
			RemoveConsignmentMerchant();

			// set porch to false, no mo porch!
			Porch = false;

			// broadcast updates
			SendUpdate();
			SaveIntoDatabase();

			return true;
		}

		public bool AddConsignment(long startValue)
		{
			// check to make sure a consignment merchant doesn't already exist for this house.
			if (ConsignmentMerchant != null)
			{
				if (log.IsDebugEnabled)
					log.DebugFormat("Add CM: House {0} already has a consignment merchant.", HouseNumber);

				return false;
			}

			var houseCM = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
			if (houseCM != null)
			{
				if (log.IsDebugEnabled)
					log.DebugFormat("Add CM: Found active consignment merchant in HousingConsignmentMerchant table for house {0}.", HouseNumber);

				return false;
			}

			var obj = DOLDB<DbMob>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
			if (obj != null)
			{
				if (log.IsDebugEnabled)
					log.DebugFormat("Add CM: Found consignment merchant in Mob table for house {0} but none in HousingConsignmentMerchant!  Creating a new merchant.", HouseNumber);

				GameServer.Database.DeleteObject(obj);
			}

			if (DatabaseItem.HasConsignment == true)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("Add CM: No Consignment Merchant found but House DB record HasConsignment for house {0}!  Creating a new merchant.", HouseNumber);
			}

			// now let's try to find a CM with this owner ID and no house and if we find it, attach
			houseCM = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("OwnerID").IsEqualTo(OwnerID));

			if (houseCM != null)
			{
				if (log.IsWarnEnabled)
					log.Warn($"Re-adding an existing consignment merchant for house {HouseNumber}. The previous house was {houseCM.HouseNumber}");

				houseCM.HouseNumber = HouseNumber;
				GameServer.Database.SaveObject(houseCM);
			}
			else
			{
				// create a new consignment merchant entry, and add it to the DB
				if (log.IsWarnEnabled)
					log.Warn("Adding a consignment merchant for house " + HouseNumber);

				houseCM = new DbHouseConsignmentMerchant { OwnerID = OwnerID, HouseNumber = HouseNumber, Money = startValue };
				GameServer.Database.AddObject(houseCM);
			}

			float[] consignmentCoords = HousingConstants.ConsignmentPositioning[Model];
			double multi = consignmentCoords[0];
			var range = (int) consignmentCoords[1];
			var zaddition = (int) consignmentCoords[2];
			var realm = (int) consignmentCoords[3];

			double angle = Heading*((Math.PI*2)/360); // angle*2pi/360;
			var heading = (ushort) ((Heading < 180 ? Heading + 180 : Heading - 180)/0.08789);
			var tX = (int) ((X + (500*Math.Sin(angle))) - Math.Sin(angle - multi)*range);
			var tY = (int) ((Y - (500*Math.Cos(angle))) + Math.Cos(angle - multi)*range);

			GameConsignmentMerchant con = GameServer.ServerRules.CreateHousingConsignmentMerchant(this);

			con.CurrentRegionID = RegionID;
			con.X = tX;
			con.Y = tY;
			con.Z = Z + zaddition;
			con.Level = 50;
			con.Realm = (eRealm) realm;
			con.HouseNumber = (ushort)HouseNumber;
			con.Heading = heading;
			con.Model = 144;

			con.Flags |= GameNPC.eFlags.PEACE;
			con.LoadedFromScript = false;
			con.RoamingRange = 0;

			if (DatabaseItem.GuildHouse)
				con.GuildName = DatabaseItem.GuildName;

			con.AddToWorld();
			con.SaveIntoDatabase();

			DatabaseItem.HasConsignment = true;
			SaveIntoDatabase();

			return true;
		}

		public bool RemoveConsignmentMerchant()
		{
			if (ConsignmentMerchant == null)
				return false;

			// If this is a guild house and the house is removed the items still belong to the guild ID and will show up
			// again if guild purchases another house and CM

			int count = 0;
			foreach(DbInventoryItem item in ConsignmentMerchant.GetDbItems(null))
			{
				item.OwnerLot = 0;
				GameServer.Database.SaveObject(item);
				count++;
			}

			var houseCM = DOLDB<DbHouseConsignmentMerchant>.SelectObject(DB.Column("HouseNumber").IsEqualTo(HouseNumber));
			if (houseCM != null)
			{
				houseCM.HouseNumber = 0;
				GameServer.Database.SaveObject(houseCM);
			}

			ConsignmentMerchant.HouseNumber = 0;
			ConsignmentMerchant.DeleteFromDatabase();
			ConsignmentMerchant.Delete();

			ConsignmentMerchant = null;
			DatabaseItem.HasConsignment = false;

			SaveIntoDatabase();
			return true;
		}

		public void PickUpConsignmentMerchant(GamePlayer player)
		{
			if (!CanEmptyHookpoint(player))
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

			InventoryLogging.LogInventoryAction("(HOUSE;" + HouseNumber + ")", player, eInventoryActionType.Loot, itemTemplate);
		}

		public void Edit(GamePlayer player, List<int> changes)
		{
			MerchantTradeItems items = player.InHouse ? HouseTemplateMgr.IndoorMenuItems : HouseTemplateMgr.OutdoorMenuItems;
			if (items == null)
				return;

			if (!player.InHouse)
			{
				if (!CanChangeExternalAppearance(player))
					return;
			}
			else
			{
				if (!CanChangeInterior(player, DecorationPermissions.Add))
					return;
			}

			long price = 0;

			// tally up the price for all the requested changes
			foreach (int slot in changes)
			{
				int page = slot/30;
				int pslot = slot%30;

				DbItemTemplate item = items.GetItem(page, (eMerchantWindowSlot) pslot);
				if (item != null)
				{
					price += item.Price;
				}
			}

			// make sure player has enough money to cover the changes
			if (!player.RemoveMoney(price))
			{
                InventoryLogging.LogInventoryAction(player, "(HOUSE;" + HouseNumber + ")", eInventoryActionType.Merchant, price);
				ChatUtil.SendMerchantMessage(player, "House.Edit.NotEnoughMoney", null);
				return;
			}

			ChatUtil.SendSystemMessage(player, "House.Edit.PayForChanges", Money.GetString(price));

			// make all the changes
			foreach (int slot in changes)
			{
				int page = slot/30;
				int pslot = slot%30;

				DbItemTemplate item = items.GetItem(page, (eMerchantWindowSlot) pslot);

				if (item != null)
				{
					switch ((eObjectType) item.Object_Type)
					{
						case eObjectType.HouseInteriorBanner:
							IndoorGuildBanner = (item.DPS_AF == 1 ? true : false);
							break;
						case eObjectType.HouseInteriorShield:
							IndoorGuildShield = (item.DPS_AF == 1 ? true : false);
							break;
						case eObjectType.HouseCarpetFirst:
							Rug1Color = item.DPS_AF;
							break;
						case eObjectType.HouseCarpetSecond:
							Rug2Color = item.DPS_AF;
							break;
						case eObjectType.HouseCarpetThird:
							Rug3Color = item.DPS_AF;
							break;
						case eObjectType.HouseCarpetFourth:
							Rug4Color = item.DPS_AF;
							break;
						case eObjectType.HouseTentColor:
							PorchRoofColor = item.DPS_AF;
							break;
						case eObjectType.HouseExteriorBanner:
							OutdoorGuildBanner = (item.DPS_AF == 1 ? true : false);
							break;
						case eObjectType.HouseExteriorShield:
							OutdoorGuildShield = (item.DPS_AF == 1 ? true : false);
							break;
						case eObjectType.HouseRoofMaterial:
							RoofMaterial = item.DPS_AF;
							break;
						case eObjectType.HouseWallMaterial:
							WallMaterial = item.DPS_AF;
							break;
						case eObjectType.HouseDoorMaterial:
							DoorMaterial = item.DPS_AF;
							break;
						case eObjectType.HousePorchMaterial:
							PorchMaterial = item.DPS_AF;
							break;
						case eObjectType.HouseWoodMaterial:
							TrussMaterial = item.DPS_AF;
							break;
						case eObjectType.HouseShutterMaterial:
							WindowMaterial = item.DPS_AF;
							break;
						default:
							break; //dirty work a round - dont know how mythic did it, hardcoded? but it works
					}
				}
			}

			// save the house
			GameServer.Database.SaveObject(_databaseItem);
			
			SendUpdate();
		}

		#endregion

		#region Permissions

		#region Add/Remove/Edit

		public bool AddPermission(GamePlayer player, PermissionType permType, int permLevel)
		{
			// make sure player is not null
			if (player == null)
				return false;

			// get the proper target name (acct name or player name)
			string targetName = permType == PermissionType.Account ? player.Client.Account.Name : player.Name;

			//  check to make sure an existing mapping doesn't exist.
			foreach (DbHouseCharsXPerms perm in _housePermissions.Values)
			{
				// fast expression to evaluate to match appropriate permissions
				if (perm.PermissionType == (int) permType)
				{
					// make sure it's not identical, which would mean we couldn't add!
					if (perm.TargetName == targetName)
						return false;
				}
			}

			// no matching permissions, create a new one and add it.
			var housePermission = new DbHouseCharsXPerms(HouseNumber, targetName, player.Name, permLevel, (int) permType);
			GameServer.Database.AddObject(housePermission);

			// add it to our list
			_housePermissions.Add(GetOpenPermissionSlot(), housePermission);

			return true;
		}

		public bool AddPermission(string targetName, PermissionType permType, int permLevel)
		{
			//  check to make sure an existing mapping doesn't exist.
			foreach (DbHouseCharsXPerms perm in _housePermissions.Values)
			{
				// fast expression to evaluate to match appropriate permissions
				if (perm.PermissionType == (int) permType)
				{
					// make sure it's not identical, which would mean we couldn't add!
					if (perm.TargetName == targetName)
						return false;
				}
			}

			// no matching permissions, create a new one and add it.
			var housePermission = new DbHouseCharsXPerms(HouseNumber, targetName, targetName, permLevel, (int) permType);
			GameServer.Database.AddObject(housePermission);

			// add it to our list
			_housePermissions.Add(GetOpenPermissionSlot(), housePermission);

			return true;
		}

		/// <summary>
		/// Grabs the first available house permission slot.
		/// </summary>
		/// <returns>returns an open house permission slot as an int</returns>
		private int GetOpenPermissionSlot()
		{
			int startingSlot = 0;

			while(_housePermissions.ContainsKey(startingSlot))
			{
				startingSlot++;
			}

			return startingSlot;
		}

		public void RemovePermission(int slot)
		{
			// make sure the permission exists
			if (!_housePermissions.TryGetValue(slot, out DbHouseCharsXPerms matchedPerm))
				return;

			// remove the permission and delete it from the database
			_housePermissions.Remove(slot);
			GameServer.Database.DeleteObject(matchedPerm);
		}

		public void AdjustPermissionSlot(int slot, int newPermLevel)
		{
			// make sure the permission exists
			if (!_housePermissions.TryGetValue(slot, out DbHouseCharsXPerms permission))
				return;

			// check for proper permission level range
			if (newPermLevel is < HousingConstants.MinPermissionLevel or > HousingConstants.MaxPermissionLevel)
				return;

			// update the permission level
			permission.PermissionLevel = newPermLevel;

			// save the permission
			GameServer.Database.SaveObject(permission);
		}

		#endregion

		#region Get Permissions

		private DbHouseCharsXPerms GetPlayerPermissions(GamePlayer player)
		{
			// make sure player isn't null
			if (player == null)
				return null;

			// try character permissions first
			IEnumerable<DbHouseCharsXPerms> charPermissions = from cp in _housePermissions.Values
				where
				cp.TargetName == player.Name &&
				cp.PermissionType == (int) PermissionType.Player
				select cp;

			if (charPermissions.Count() > 0)
				return charPermissions.First();

			// try account permissions next
			IEnumerable<DbHouseCharsXPerms> acctPermissions = from cp in _housePermissions.Values
				where
				cp.TargetName == player.Client.Account.Name &&
				cp.PermissionType == (int) PermissionType.Account
				select cp;

			if (acctPermissions.Count() > 0)
				return acctPermissions.First();

			if (player.Guild != null)
			{
				// try guild permissions next
				IEnumerable<DbHouseCharsXPerms> guildPermissions = from cp in _housePermissions.Values
					where
					player.Guild.Name == cp.TargetName &&
					cp.PermissionType == (int) PermissionType.Guild
					select cp;

				if (guildPermissions.Count() > 0)
					return guildPermissions.First();
			}

			// look for the catch-all permissions last
			IEnumerable<DbHouseCharsXPerms> allPermissions = from cp in _housePermissions.Values
				where cp.TargetName == "All"
				select cp;

			if (allPermissions.Count() > 0)
				return allPermissions.First();

			// nothing found, return null
			return null;
		}

		private bool HasAccess(GamePlayer player, Func<DbHousePermissions, bool> accessExpression)
		{
			// make sure player isn't null
			if (player == null)
				return false;

			// owner and GMs+ can do everything
			if (HasOwnerPermissions(player))
				return true;

			// get house permissions for the given player
			DbHousePermissions housePermissions = GetPermissionLevel(player);

			if (housePermissions == null)
				return false;

			// get result of the permission check expression
			return accessExpression(housePermissions);
		}

		private DbHousePermissions GetPermissionLevel(GamePlayer player)
		{
			// get player permissions mapping
			DbHouseCharsXPerms permissions = GetPlayerPermissions(player);

			if (permissions == null)
				return null;

			// get house permissions for the given mapping
			DbHousePermissions housePermissions = GetPermissionLevel(permissions);

			return housePermissions;
		}

		private DbHousePermissions GetPermissionLevel(DbHouseCharsXPerms charPerms)
		{
			// make sure permissions aren't null
			if (charPerms == null)
				return null;

			return GetPermissionLevel(charPerms.PermissionLevel);
		}

		private DbHousePermissions GetPermissionLevel(int permissionLevel)
		{
			DbHousePermissions permissions;
			_permissionLevels.TryGetValue(permissionLevel, out permissions);

			return permissions;
		}

		public bool HasOwnerPermissions(GamePlayer player)
		{
			// make sure player isn't null
			if (player == null)
				return false;

			if (player.Client.Account.PrivLevel == (int)ePrivLevel.Admin)
				return true;

			// check by character name/account if not guild house
			if (!_databaseItem.GuildHouse)
			{
				// check if character is explicit owner
				if (_databaseItem.OwnerID == player.ObjectId)
					return true;

				// check account-wide if not a guild house
				IEnumerable<DbCoreCharacter> charsOnAccount = from chr in player.Client.Account.Characters
					where chr.ObjectId == _databaseItem.OwnerID
					select chr;

				if (charsOnAccount.Count() > 0)
					return true;
			}

			// check based on guild
			if (player.Guild != null)
			{
				return OwnerID == player.Guild.GuildID && player.Guild.HasRank(player, Guild.eRank.Leader);
			}

			// no character/account/guild match, not an owner
			return false;
		}

		#endregion

		#region Check Permissions

		public bool CanEmptyHookpoint(GamePlayer player)
		{
			return HasOwnerPermissions(player);
		}

		public bool CanEnterHome(GamePlayer player)
		{
			// check if player has access
			return HasAccess(player, cp => cp.CanEnterHouse);
		}

		public bool CanUseVault(GamePlayer player, GameHouseVault vault, VaultPermissions vaultPerms)
		{
			// make sure player isn't null
			if (player == null || player.CurrentHouse != this)
				return false;

			if (HasOwnerPermissions(player))
				return true;

			// get player house permissions
			DbHousePermissions housePermissions = GetPermissionLevel(player);

			if (housePermissions == null)
				return false;

			// get the vault permissions for the given vault
			VaultPermissions activeVaultPermissions = VaultPermissions.None;

			switch (vault.Index)
			{
				case 0:
					activeVaultPermissions = (VaultPermissions) housePermissions.Vault1;
					break;
				case 1:
					activeVaultPermissions = (VaultPermissions) housePermissions.Vault2;
					break;
				case 2:
					activeVaultPermissions = (VaultPermissions) housePermissions.Vault3;
					break;
				case 3:
					activeVaultPermissions = (VaultPermissions) housePermissions.Vault4;
					break;
			}

			ChatUtil.SendDebugMessage(player, string.Format("Vault permissions = {0} for vault index {1}", (activeVaultPermissions & vaultPerms), vault.Index));

			return (activeVaultPermissions & vaultPerms) > 0;
		}

		public bool CanUseConsignmentMerchant(GamePlayer player, ConsignmentPermissions consignPerms)
		{
			// make sure player isn't null
			if (player == null)
				return false;

			// owner and Admins can do everything
			if (HasOwnerPermissions(player))
				return true;

			// get player house permissions
			DbHousePermissions housePermissions = GetPermissionLevel(player);

			if (housePermissions == null)
				return false;

			return ((ConsignmentPermissions) housePermissions.ConsignmentMerchant & consignPerms) > 0;
		}

		public bool CanChangeInterior(GamePlayer player, DecorationPermissions interiorPerms)
		{
			// make sure player isn't null
			if (player == null)
				return false;

			// owner and GMs+ can do everything
			if (HasOwnerPermissions(player) || player.Client.Account.PrivLevel > 1)
				return true;

			// get player house permissions
			DbHousePermissions housePermissions = GetPermissionLevel(player);

			if (housePermissions == null)
				return false;

			return ((DecorationPermissions) housePermissions.ChangeInterior & interiorPerms) > 0;
		}

		public bool CanChangeGarden(GamePlayer player, DecorationPermissions gardenPerms)
		{
			// make sure player isn't null
			if (player == null)
				return false;

			// owner and GMs+ can do everything
			if (HasOwnerPermissions(player) || player.Client.Account.PrivLevel > 1)
				return true;

			// get player house permissions
			DbHousePermissions housePermissions = GetPermissionLevel(player);

			if (housePermissions == null)
				return false;

			return ((DecorationPermissions) housePermissions.ChangeGarden & gardenPerms) > 0;
		}

		public bool CanChangeExternalAppearance(GamePlayer player)
		{
			// check if player has access
			return HasAccess(player, cp => cp.CanChangeExternalAppearance);
		}

		public bool CanBanish(GamePlayer player)
		{
			// check if player has access
			return HasAccess(player, cp => cp.CanBanish);
		}

		public bool CanBindInHouse(GamePlayer player)
		{
			// check if player has access
			return HasAccess(player, cp => cp.CanBindInHouse);
		}

		public bool CanPayRent(GamePlayer player)
		{
			// check if player has access
			return HasAccess(player, cp => cp.CanPayRent);
		}

		public bool CanUseMerchants(GamePlayer player)
		{
			// check if player has access
			return HasAccess(player, cp => cp.CanUseMerchants);
		}

		public bool CanUseTools(GamePlayer player)
		{
			// check if player has access
			return HasAccess(player, cp => cp.CanUseTools);
		}

		#endregion

		#endregion

		#region Database

		/// <summary>
		/// Saves this house into the database
		/// </summary>
		public void SaveIntoDatabase()
		{
			GameServer.Database.SaveObject(_databaseItem);
		}

		/// <summary>
		/// Load a house from the database
		/// </summary>
		public void LoadFromDatabase()
		{
			int i = 0;
			_indoorItems.Clear();
			foreach (DbHouseIndoorItem dbiitem in DOLDB<DbHouseIndoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
			{
				_indoorItems.Add(i++, new IndoorItem(dbiitem));
			}

			i = 0;
			_outdoorItems.Clear();
			foreach (DbHouseOutdoorItem dboitem in DOLDB<DbHouseOutdoorItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
			{
				_outdoorItems.Add(i++, new OutdoorItem(dboitem));
			}

			_housePermissions.Clear();
			foreach (DbHouseCharsXPerms d in DOLDB<DbHouseCharsXPerms>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
			{
				_housePermissions.Add(GetOpenPermissionSlot(), d);
			}

			_permissionLevels.Clear();
			foreach (DbHousePermissions dbperm in DOLDB<DbHousePermissions>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
			{
				if (_permissionLevels.ContainsKey(dbperm.PermissionLevel) == false)
					_permissionLevels.Add(dbperm.PermissionLevel, dbperm);
				else if (log.IsErrorEnabled)
					log.ErrorFormat("Duplicate permission level {0} for house {1}", dbperm.PermissionLevel, HouseNumber);
			}

			HousepointItems.Clear();
			foreach (DbHouseHookPointItem item in DOLDB<DbHouseHookPointItem>.SelectObjects(DB.Column("HouseNumber").IsEqualTo(HouseNumber)))
			{
				if (HousepointItems.ContainsKey(item.HookpointID) == false)
				{
					HousepointItems.Add(item.HookpointID, item);
					FillHookpoint(item.HookpointID, item.ItemTemplateID, item.Heading, item.Index);
				}
				else if (log.IsErrorEnabled)
					log.ErrorFormat("Duplicate item {0} attached to hookpoint {1} for house {2}!", item.ItemTemplateID, item.HookpointID, HouseNumber);
			}
		}

		#endregion
	}
}