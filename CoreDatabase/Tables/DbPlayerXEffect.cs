using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Account table
	/// </summary>
	[DataTable(TableName = "PlayerXEffect")]
	public class DbPlayerXEffect : DataObject
	{
		private string m_charid;
		private string m_effecttype;
		private bool m_ishandler;
		private int m_duration;
		private int m_var1;
		private double m_var2;
		private int m_var3;
		private int m_var4;
		private int m_var5;
		private int m_var6;
		private string m_spellLine;

		public DbPlayerXEffect()
		{
		}

		[DataElement(AllowDbNull = false)]
		public bool IsHandler
		{
			get
			{
				return m_ishandler;
			}
			set
			{
				Dirty = true;
				m_ishandler = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Var6
		{
			get
			{
				return m_var6;
			}
			set
			{
				Dirty = true;
				m_var6 = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Var5
		{
			get
			{
				return m_var5;
			}
			set
			{
				Dirty = true;
				m_var5 = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Var4
		{
			get
			{
				return m_var4;
			}
			set
			{
				Dirty = true;
				m_var4 = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Var3
		{
			get
			{
				return m_var3;
			}
			set
			{
				Dirty = true;
				m_var3 = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public double Var2
		{
			get
			{
				return m_var2;
			}
			set
			{
				Dirty = true;
				m_var2 = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Var1
		{
			get
			{
				return m_var1;
			}
			set
			{
				Dirty = true;
				m_var1 = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Duration
		{
			get
			{
				return m_duration;
			}
			set
			{
				Dirty = true;
				m_duration = value;
			}
		}


		[DataElement(AllowDbNull = true)]
		public string EffectType
		{
			get { return m_effecttype; }
			set
			{
				Dirty = true;
				m_effecttype = value;
			}
		}

		[DataElement(AllowDbNull = true)]
		public string SpellLine
		{
			get { return m_spellLine; }
			set
			{
				Dirty = true;
				m_spellLine = value;
			}
		}

		[DataElement(AllowDbNull = true, Index = true)]
		public string ChardID
		{
			get
			{
				return m_charid;
			}
			set
			{
				Dirty = true;
				m_charid = value;
			}
		}
	}
}
