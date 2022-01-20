/*
 * Account Vault Keeper - Kakuri Mar 20 2009
 * A fake GameHouseVault that works as an account vault.
 * The methods and properties of GameHouseVault *must* be marked as virtual for this to work (which was not the case in DOL builds prior to 1584).
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class AccountVaultKeeper : GameNPC
    {
        private bool oldAcc = false;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;
            
            if (player.HCFlag)
            {
                SayTo(player,$"I'm sorry {player.Name}, my vault is not Hardcore enough for you.");
                return false;
            }

            ItemTemplate vaultItem = GetDummyVaultItem(player);
            AccountVault vault = new AccountVault(player, this, player.Client.Account.Name, 0, vaultItem);
            player.ActiveInventoryObject = vault;
            //player.ActiveVault = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;
            
            if (player.HCFlag)
            {
                SayTo(player,$"I'm sorry {player.Name}, my vault is not Hardcore enough for you.");
                return false;
            }
            
            if (player.NoHelp)
            {
                SayTo(player,$"I'm sorry {player.Name}, you have chosen the path of solitude.");
                return false;
            }

            return true;
        }


        private static ItemTemplate GetDummyVaultItem(GamePlayer player)
        {
            ItemTemplate vaultItem = new ItemTemplate();
            vaultItem.Object_Type = (int)eObjectType.HouseVault;
            vaultItem.Id_nb = "alb_vault";
            vaultItem.Name = "Albion Vault";
            vaultItem.Model = 1489;

            switch (player.Realm)
            {
                /*case eRealm.Albion:
                    vaultItem.Id_nb = "alb_vault";
                    vaultItem.Name = "Albion Vault";
                    vaultItem.Model = 1489;
                    break;*/
                case eRealm.Hibernia:
                    vaultItem.Id_nb = "hib_vault";
                    vaultItem.Name = "Hibernia Vault";
                    vaultItem.Model = 1491;
                    break;
                case eRealm.Midgard:
                    vaultItem.Id_nb = "mid_vault";
                    vaultItem.Name = "Midgard Vault";
                    vaultItem.Model = 1493;
                    break;
            }

            return vaultItem;
        }

        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            switch (Realm)
            {
                case eRealm.Albion:
                    Name = "Virsha";
                    Model = 45;
                    Size = 50;
                    template.AddNPCEquipment(eInventorySlot.HeadArmor, 1290);
                    template.AddNPCEquipment(eInventorySlot.HandsArmor, 691);
                    template.AddNPCEquipment(eInventorySlot.ArmsArmor, 690);
                    template.AddNPCEquipment(eInventorySlot.TorsoArmor, 688);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 689);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 692);
                    template.AddNPCEquipment(eInventorySlot.Cloak, 557);
                    break;

                case eRealm.Midgard:
                    Name = "Janash";
                    Model = 161;
                    Size = 50;
                    template.AddNPCEquipment(eInventorySlot.HeadArmor, 822);
                    template.AddNPCEquipment(eInventorySlot.HandsArmor, 80);
                    template.AddNPCEquipment(eInventorySlot.ArmsArmor, 789);
                    template.AddNPCEquipment(eInventorySlot.TorsoArmor, 787);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 788);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 791);
                    template.AddNPCEquipment(eInventorySlot.Cloak, 55);
                    break;

                case eRealm.Hibernia:
                    Name = "Giorny";
                    Model = 312;
                    Size = 50;
                    template.AddNPCEquipment(eInventorySlot.HeadArmor, 1292);
                    template.AddNPCEquipment(eInventorySlot.HandsArmor, 785);
                    template.AddNPCEquipment(eInventorySlot.ArmsArmor, 784);
                    template.AddNPCEquipment(eInventorySlot.TorsoArmor, 782);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 783);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 786);
                    template.AddNPCEquipment(eInventorySlot.Cloak, 57);
                    break;
            }

            Inventory = template.CloseTemplate();
            Flags |= eFlags.PEACE;
            GuildName = "Account Vault Keeper";
            Level = 75;

            return base.AddToWorld();
        }
    }

    public class AccountVault : GameHouseVault
    {
        public const int SIZE = 100;
        public const int FIRST_SLOT = 1600;

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private GamePlayer m_player;
        private GameNPC m_vaultNPC;
        private string m_vaultOwner;
        private int m_vaultNumber = 0;

        private object m_vaultSync = new object();


        /// <summary>
        /// An account vault that masquerades as a house vault to the game client
        /// </summary>
        /// <param name="player">Player who owns the vault</param>
        /// <param name="vaultNPC">NPC controlling the interaction between player and vault</param>
        /// <param name="vaultOwner">ID of vault owner (can be anything unique, if it's the account name then all toons on account can access the items)</param>
        /// <param name="vaultNumber">Valid vault IDs are 0-3</param>
        /// <param name="dummyTemplate">An ItemTemplate to satisfy the base class's constructor</param>
        public AccountVault(GamePlayer player, GameNPC vaultNPC, string vaultOwner, int vaultNumber,
            ItemTemplate dummyTemplate)
            : base(dummyTemplate, vaultNumber)
        {
            m_player = player;
            m_vaultNPC = vaultNPC;
            m_vaultOwner = vaultOwner;
            m_vaultNumber = vaultNumber;

            DBHouse dbh = new DBHouse();
            //was allowsave = false but uhh i replaced with allowadd = false
            dbh.AllowAdd = false;
            dbh.GuildHouse = false;
            dbh.HouseNumber = player.ObjectID;
            dbh.Name = player.Name + "'s House";
            dbh.OwnerID = player.ObjectId;
            dbh.RegionID = player.CurrentRegionID;

            CurrentHouse = new House(dbh);
        }

        public override bool CanAddItems(GamePlayer player)
        {
            return CurrentHouse.Name == player.Name + "'s House";
        }

        public override bool CanRemoveItems(GamePlayer player)
        {
            return CurrentHouse.Name == player.Name + "'s House";
        }

        private IList<InventoryItem> GetItems()
        {
            string sqlQuery = "OwnerID = '" + m_vaultOwner + "' AND ";
            sqlQuery += "SlotPosition >= " + FirstDBSlot + " AND ";
            sqlQuery += "SlotPosition <= " + LastDBSlot;

            return (GameServer.Database.SelectObjects<InventoryItem>(sqlQuery));
        }

        public override string GetOwner(GamePlayer player)
        {
            return player.Client.Account.Name + player.Realm.ToString();
        }

        protected override void NotifyObservers(GamePlayer player, IDictionary<int, InventoryItem> updateItems)
        {
            if (!player.IsWithinRadius(m_vaultNPC, WorldMgr.INTERACT_DISTANCE))
            {
                player.Out.SendMessage("You are out of range of the vault keeper.", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                player.ActiveInventoryObject = null;
            }

            player.Out.SendInventoryItemsUpdate(updateItems, eInventoryWindowType.Update);
        }

        public string VaultOwner
        {
            get { return m_vaultOwner; }
            set { m_vaultOwner = value; }
        }
    }
}
