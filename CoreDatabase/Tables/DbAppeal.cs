using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    /// Saves an Appeal
    /// </summary>
    [DataTable(TableName = "Appeal")]
    public class DbAppeal : DataObject
    {
        private string m_name;
        private string m_account;
        private int m_severity;
        private string m_status;
        private string m_timestamp;
        private string m_text;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Player's name</param>
        /// <param name="severity">The severity of the appeal (low, medium, high, critical)</param>
        /// <param name="status">The status of the appeal (Open, Being Helped)</param>
        /// <param name="timestamp">When the appeal was first created</param>
        /// <param name="text">Content of the appeal (text)</param>
        public DbAppeal()
        {
            m_name = string.Empty;
            m_account = string.Empty;
            m_severity = 0;
            m_status = string.Empty;
            m_timestamp = string.Empty;
            m_text = string.Empty;
        }

        public DbAppeal(string name, string account, int severity, string status, string timestamp, string text)
        {
            m_name = name;
            m_account = account;
            m_severity = severity;
            m_status = status;
            m_timestamp = timestamp;
            m_text = text;
        }

        [DataElement(AllowDbNull = false, Index = true)]
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        [DataElement(AllowDbNull = false, Index = true)]
        public string Account
        {
            get { return m_account; }
            set { m_account = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int Severity
        {
            get { return m_severity; }
            set { m_severity = value; }
        }

        public string SeverityToName
        {
            get
            {
                switch (Severity)
                {
                    case 1:
                        return "Low";
                    case 2:
                        return "Medium";
                    case 3:
                        return "High";
                    case 4:
                        return "Critical";
                    default:
                        return "none";
                }
            }
            set { }
        }

        [DataElement(AllowDbNull = false)]
        public string Status
        {
            get { return m_status; }
            set { m_status = value; }
        }

        [DataElement(AllowDbNull = false)]
        public string Timestamp
        {
            get { return m_timestamp; }
            set { m_timestamp = value; }
        }
        [DataElement(AllowDbNull = false)]
        public string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }
    }
}
