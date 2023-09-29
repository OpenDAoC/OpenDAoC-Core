using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// The salvage table
	/// </summary>
	[DataTable(TableName="Salvage")]
	public class DbSalvage : DataObject
	{
		private int m_objectType;
		private int m_salvageLevel;
		private string m_id_nb;
        private int m_realm;

		/// <summary>
		/// Create salvage
		/// </summary>
		public DbSalvage()
		{
			AllowAdd =false;
		}

		/// <summary>
		/// Object type of item to salvage
		/// </summary>
		[DataElement(AllowDbNull=false, Index = true)]
		public int ObjectType
		{
			get
			{
				return m_objectType;
			}
			set
			{
				Dirty = true;
				m_objectType = value;
			}
		}

		/// <summary>
		/// The salvage level of the row
		/// </summary>
		[DataElement(AllowDbNull=false, Index = true)]
		public int SalvageLevel
		{
			get
			{
				return m_salvageLevel;
			}
			set
			{
				Dirty = true;
				m_salvageLevel = value;
			}
		}

		/// <summary>
		/// Index of item to craft
		/// </summary>
		[DataElement(AllowDbNull=false)]
		public string Id_nb
		{
			get
			{
				return m_id_nb;
			}
			set
			{
				Dirty = true;
				m_id_nb = value;
			}
		}

        /// <summary>
        /// Realm of item to salvage
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true)]
        public int Realm
        {
            get
            {
                return m_realm;
            }
            set
            {
                Dirty = true;
                m_realm = value;
            }
        }

		/// <summary>
		/// The raw material to give when salvage
		/// </summary>
		[Relation(LocalField = "Id_nb", RemoteField = "Id_nb", AutoLoad = true, AutoDelete=false)]
		public DbItemTemplate RawMaterial;
	}
}
