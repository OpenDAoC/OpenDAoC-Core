using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    /// Contains all Consignment Merchants and owners
    /// </summary>
    [DataTable(TableName = "HouseConsignmentMerchant")]
    public class DbHouseConsignmentMerchant : DataObject
    {
		long m_ID;
		private string m_ownerID;
        private int m_houseNumber;
        private long m_money;


        public DbHouseConsignmentMerchant()
        {
			m_ownerID = string.Empty;
			m_houseNumber = 0;
			m_money = 0;
        }

		[PrimaryKey(AutoIncrement = true)]
		public long ID
		{
			get { return m_ID; }
			set
			{
				Dirty = true;
				m_ID = value;
			}
		}

		/// <summary>
		/// The owner id of this merchant.  Can be player or guild.
		/// </summary>
		[DataElement(AllowDbNull = false, Varchar=128, Index=true)]
		public string OwnerID
		{
			get
			{
				return m_ownerID;
			}
			set
			{
				Dirty = true;
				m_ownerID = value;
			}
		}

        /// <summary>
        /// The Housenumber of the Merchant
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true)]
        public int HouseNumber
        {
            get
            {
                return m_houseNumber;
            }
            set
            {
                Dirty = true;
                m_houseNumber = value;
            }
        }


        /// <summary>
        /// The value of the money/bp the merchant currently holds
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public long Money
        {
            get
            {
                return m_money;
            }
            set
            {
                Dirty = true;
                m_money = value;
            }
        }
    }
}
