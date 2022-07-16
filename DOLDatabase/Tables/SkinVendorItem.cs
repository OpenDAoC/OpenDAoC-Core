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

        protected string m_skinVendorItemId;
        [DataElement(AllowDbNull = false, Index = true)]
        public string SkinVendorItemId
        {
            get { return m_skinVendorItemId; }
            set { Dirty = false; m_skinVendorItemId = value; }
        }
        protected string m_name;
        [DataElement(AllowDbNull = false, Index = false)]
        public string Name
        {
            get { return m_name; }
            set { Dirty = false; m_name = value; }
        }
        protected int m_modelId;
        [DataElement(AllowDbNull = false, Index = false)]
        public int ModelId
        {
            get { return m_modelId; }
            set { Dirty = false; m_modelId = value; }
        }

        protected int m_itemType;
        [DataElement(AllowDbNull = false, Index = false)]
        public int ItemType
        {
            get { return m_itemType; }
            set { Dirty = false; m_itemType = value; }
        }

        protected int m_realmRank;
        [DataElement(AllowDbNull = false, Index = false)]
        public int RealmRank
        {
            get { return m_realmRank; }
            set { Dirty = false; m_realmRank = value; }
        }
        protected int m_drake;
        [DataElement(AllowDbNull = false, Index = false)]
        public int Drake
        {
            get { return m_drake; }
            set { Dirty = false; m_drake = value; }
        }
        protected int m_orbs;
        [DataElement(AllowDbNull = false, Index = false)]
        public int Orbs
        {
            get { return m_orbs; }
            set { Dirty = false; m_orbs = value; }
        }
        protected int m_realm;
        [DataElement(AllowDbNull = false, Index = false)]
        public int Realm
        {
            get { return m_realm; }
            set { Dirty = false; m_realm = value; }
        }
        protected int m_characterClass;
        [DataElement(AllowDbNull = false, Index = false)]
        public int CharacterClass
        {
            get { return m_characterClass; }
            set { Dirty = false; m_characterClass = value; }
        }
        protected int m_objectType;
        [DataElement(AllowDbNull = false, Index = false)]
        public int ObjectType
        {
            get { return m_objectType; }
            set { Dirty = false; m_objectType = value; }
        }
        protected int m_damageType;
        [DataElement(AllowDbNull = false, Index = false)]
        public int DamageType
        {
            get { return m_damageType; }
            set { Dirty = false; m_damageType = value; }
        }

        protected int m_price;
        [DataElement(AllowDbNull = false, Index = false)]
        public int Price
        {
            get { return m_price; }
            set { Dirty = false; m_price = value; }
        }

        public SkinVendorItem(string skinVendorItemId, string name, int modelId, int itemType, int realmRank, int drake, int orbs, int realm, int characterClass, int objectType,int damagetype, int price)
        {
            SkinVendorItemId = skinVendorItemId;
            Name = name;
            ModelId = modelId;
            ItemType = itemType;
            RealmRank = realmRank;
            Drake = drake;
            Orbs = orbs;
            Realm = realm;
            CharacterClass = characterClass;
            ObjectType = objectType;
            DamageType = damagetype;
            Price = price;
        }



        #endregion
    }
}
