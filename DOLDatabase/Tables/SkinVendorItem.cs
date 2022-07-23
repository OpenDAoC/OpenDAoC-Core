using DOL.Database;
using DOL.Database.Attributes;
using log4net;
using System.Reflection;

namespace DOLDatabase.Tables
{
    /// <summary>
    /// The InventoryItem table holds all Items of the SkinVendor
    /// </summary>

    [DataTable(TableName = "SkinVendorItems")]
    public class SkinVendorItem : DataObject
    {

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected bool m_hasLoggedError = false;
        #region Inventory fields
        
        protected int m_SkinVendorItemID;
        [PrimaryKey(AutoIncrement = true)]
        public int SkinVendorItemID
        {
            get { return m_SkinVendorItemID; }
            set {  m_SkinVendorItemID = value; }
        }

        protected string m_name;
        [DataElement(AllowDbNull = false, Index = false)]
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        protected int m_modelId;
        [DataElement(AllowDbNull = false, Index = false)]
        public int ModelID
        {
            get { return m_modelId; }
            set {  m_modelId = value; }
        }

        protected int m_itemType;
        [DataElement(AllowDbNull = false, Index = false)]
        public int ItemType
        {
            get { return m_itemType; }
            set {  m_itemType = value; }
        }

        protected int m_playerRealmRank;
        [DataElement(AllowDbNull = false, Index = false)]
        public int PlayerRealmRank
        {
            get { return m_playerRealmRank; }
            set { m_playerRealmRank = value; }
        }
        protected int m_accountRealmRank;

        [DataElement(AllowDbNull = false, Index = false)]
        public int AccountRealmRank
        {
            get { return m_accountRealmRank; }
            set { m_accountRealmRank = value; }
        }
        protected int m_drake;

        [DataElement(AllowDbNull = false, Index = false)]
        public int Drake
        {
            get { return m_drake; }
            set { m_drake = value; }
        }
        protected int m_orbs;
        [DataElement(AllowDbNull = false, Index = false)]
        public int Orbs
        {
            get { return m_orbs; }
            set {  m_orbs = value; }
        }


        protected int m_epicBossKills;
        [DataElement(AllowDbNull = false, Index = false)]
        public int EpicBossKills
        {
            get { return m_epicBossKills; }
            set { m_epicBossKills = value; }
        }

        protected int m_masteredCrafts;
        [DataElement(AllowDbNull = false, Index = false)]
        public int MasteredCrafts
        {
            get { return m_masteredCrafts; }
            set { m_masteredCrafts = value; }
        }

        protected int m_realm;
        [DataElement(AllowDbNull = false, Index = false)]
        public int Realm
        {
            get { return m_realm; }
            set { m_realm = value; }
        }
        protected int m_characterClass;
        [DataElement(AllowDbNull = false, Index = false)]
        public int CharacterClass
        {
            get { return m_characterClass; }
            set { m_characterClass = value; }
        }
        protected int m_objectType;
        [DataElement(AllowDbNull = false, Index = false)]
        public int ObjectType
        {
            get { return m_objectType; }
            set { m_objectType = value; }
        }
        protected int m_damageType;
        [DataElement(AllowDbNull = false, Index = false)]
        public int DamageType
        {
            get { return m_damageType; }
            set { m_damageType = value; }
        }

        protected int m_price;
        [DataElement(AllowDbNull = false, Index = false)]
        public int Price
        {
            get { return m_price; }
            set { m_price = value; }
        }

        public SkinVendorItem(string name, int modelId, int itemType, int playerRealmRank, int accountRealmRank, int drake, int orbs,int epicBossKills,int masteredCrafts, int realm, int characterClass, int objectType, int damagetype, int price)
        {
            Name = name;
            ModelID = modelId;
            ItemType = itemType;
            PlayerRealmRank = playerRealmRank;
            AccountRealmRank = accountRealmRank;
            Drake = drake;
            Orbs = orbs;
            EpicBossKills = epicBossKills;
            MasteredCrafts = masteredCrafts;
            Realm = realm;
            CharacterClass = characterClass;
            ObjectType = objectType;
            DamageType = damagetype;
            Price = price;

        }

        #endregion
    }
}
