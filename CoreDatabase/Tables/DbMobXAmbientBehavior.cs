using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Stores the triggers with corresponding text for a mob, for example if the mob should say something when it dies.
	/// </summary>
	[DataTable(TableName = "MobXAmbientBehaviour", PreCache = true)]
	public class DbMobXAmbientBehavior : DataObject
	{
		private string m_source;
		private string m_trigger;
		private ushort m_emote;
		private string m_text;
		private ushort m_chance;
		private string m_voice;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Mob's name</param>
		/// <param name="type">The type of trigger to act on (eAmbientTrigger)</param>
		/// <param name="text">The formatted text for the trigger. You can use [targetclass],[targetname],[sourcename]
		/// and supply formatting stuff: [b] for broadcast, [y] for yell</param>
		/// <param name="action">the desired emote</param>
		public DbMobXAmbientBehavior()
		{
			m_source = string.Empty;
			m_trigger =string.Empty;
			m_emote = 0;
			m_text = string.Empty;
			m_chance = 0;
			m_voice = string.Empty;
		}

		public DbMobXAmbientBehavior(string name, string trigger, ushort emote, string text, ushort chance, string voice)
		{
			m_source = name;
			m_trigger = trigger;
			m_emote = emote;
			m_text = text;
			m_chance = chance;
			m_voice = voice;
		}

		[DataElement(AllowDbNull = false, Index = true)]
		public string Source
		{
			get { return m_source; }
			set { m_source = value; }
		}

		[DataElement(AllowDbNull = false)]
		public string Trigger
		{
			get { return m_trigger; }
			set { m_trigger = value; }
		}

		[DataElement(AllowDbNull = false)]
		public ushort Emote
		{
			get { return m_emote; }
			set { m_emote = value; }
		}

		[DataElement(AllowDbNull = false)]
		public string Text
		{
			get { return m_text; }
			set { m_text = value; }
		}
	
		[DataElement(AllowDbNull = false)]
		public ushort Chance
		{
			get { return m_chance; }
			set { m_chance = value; }
		}

		[DataElement(AllowDbNull = true)]
		public string Voice
		{
			get { return m_voice; }
			set { m_voice = value; }
		}
	}
}
