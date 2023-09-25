using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable( TableName = "Race" )]
	public class DbRace : DataObject
	{
		protected int m_ID = 0;
		protected string m_Name;
		protected sbyte m_ResistBody;
		protected sbyte m_ResistCold;
		protected sbyte m_ResistCrush;
		protected sbyte m_ResistEnergy;
		protected sbyte m_ResistHeat;
		protected sbyte m_ResistMatter;
		protected sbyte m_ResistNatural;
		protected sbyte m_ResistSlash;
		protected sbyte m_ResistSpirit;
		protected sbyte m_ResistThrust;
		protected sbyte m_DamageType;

		public DbRace() : base()
		{
			AllowAdd = false;
		}

		[DataElement( AllowDbNull = false, Unique = true )]
		public int ID
		{
			get { return m_ID; }
			set
			{
				Dirty = true;
				m_ID = value;
			}
		}

		[DataElement( AllowDbNull = false, Unique = true )]
		public string Name
		{
			get { return m_Name; }
			set
			{
				Dirty = true;
				m_Name = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistBody
		{
			get { return m_ResistBody; }
			set
			{
				Dirty = true;
				m_ResistBody = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistCold
		{
			get { return m_ResistCold; }
			set
			{
				Dirty = true;
				m_ResistCold = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistCrush
		{
			get { return m_ResistCrush; }
			set
			{
				Dirty = true;
				m_ResistCrush = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistEnergy
		{
			get { return m_ResistEnergy; }
			set
			{
				Dirty = true;
				m_ResistEnergy = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistHeat
		{
			get { return m_ResistHeat; }
			set
			{
				Dirty = true;
				m_ResistHeat = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistMatter
		{
			get { return m_ResistMatter; }
			set
			{
				Dirty = true;
				m_ResistMatter = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistNatural
		{
			get { return m_ResistNatural; }
			set
			{
				Dirty = true;
				m_ResistNatural = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistSlash
		{
			get { return m_ResistSlash; }
			set
			{
				Dirty = true;
				m_ResistSlash = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistSpirit
		{
			get { return m_ResistSpirit; }
			set
			{
				Dirty = true;
				m_ResistSpirit = value;
			}
		}

		[DataElement( AllowDbNull = false)]
		public sbyte ResistThrust
		{
			get { return m_ResistThrust; }
			set
			{
				Dirty = true;
				m_ResistThrust = value;
			}
		}
	}
}