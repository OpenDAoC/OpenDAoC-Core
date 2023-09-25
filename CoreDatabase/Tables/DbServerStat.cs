using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Database Storage of ServerStats
	/// </summary>
	[DataTable(TableName = "serverstats")]
	public class DbServerStat : DataObject
	{
		protected DateTime m_statdate;
		protected int m_clients;
		protected float m_cpu;
		protected int m_upload;
		protected int m_download;
		protected long m_memory;
		protected int m_palbion;
		protected int m_pmidgard;
		protected int m_phibernia;

		public DbServerStat()
		{
			m_statdate = DateTime.Now;
			m_clients = 0;
			m_cpu = 0;
			m_upload = 0;
			m_download = 0;
			m_memory = 0;
			m_palbion = 0;
			m_pmidgard = 0;
			m_phibernia = 0;
		}

		[DataElement(AllowDbNull = false)]
		public DateTime StatDate
		{
			get { return m_statdate; }
			set
			{
				Dirty = true;
				m_statdate = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public int Clients
		{
			get { return m_clients; }
			set
			{
				Dirty = true;
				m_clients = value;
			}
		}
		
		[DataElement(AllowDbNull = false)]
		public float CPU
		{
			get { return m_cpu; }
			set
			{
				Dirty = true;
				m_cpu = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Upload
		{
			get { return m_upload; }
			set
			{
				Dirty = true;
				m_upload = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int Download
		{
			get { return m_download; }
			set
			{
				Dirty = true;
				m_download = value;
			}
		}

		[DataElement(AllowDbNull = false)]
		public long Memory
		{
			get { return m_memory; }
			set
			{
				Dirty = true;
				m_memory = value;
			}
		}
		
		[DataElement(AllowDbNull = false)]
		public int AlbionPlayers
		{
			get { return m_palbion; }
			set
			{
				Dirty = true;
				m_palbion = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int MidgardPlayers
		{
			get { return m_pmidgard; }
			set
			{
				Dirty = true;
				m_pmidgard = value;
			}
		}
		[DataElement(AllowDbNull = false)]
		public int HiberniaPlayers
		{
			get { return m_phibernia; }
			set
			{
				Dirty = true;
				m_phibernia = value;
			}
		}
	}
}