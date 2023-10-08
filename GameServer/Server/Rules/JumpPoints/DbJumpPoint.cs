using System;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS;

namespace DOL.Database
{
    [DataTable(TableName = "JumpPoint")]
    public class DbJumpPoint : DataObject
    {
        private string m_name;
        private int m_xpos;
        private int m_ypos;
        private int m_zpos;
        private ushort m_region;
        private ushort m_heading;       

        /// <summary>
        /// Name of this JP
        /// </summary>
        [DataElement(AllowDbNull = false, Unique = true)]
        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                Dirty = true;
                m_name = value;
            }
        }

        /// <summary>
        /// The region of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public ushort Region
        {
            get
            {
                return m_region;
            }
            set
            {
                Dirty = true;
                m_region = value;
            }
        }

        /// <summary>
        /// The X position of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Xpos
        {
            get
            {
                return m_xpos;
            }
            set
            {
                Dirty = true;
                m_xpos = value;
            }
        }

        /// <summary>
        /// The Y position of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Ypos
        {
            get
            {
                return m_ypos;
            }
            set
            {
                Dirty = true;
                m_ypos = value;
            }
        }

        /// <summary>
        /// The Z position of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Zpos
        {
            get
            {
                return m_zpos;
            }
            set
            {
                Dirty = true;
                m_zpos = value;
            }
        }

        /// <summary>
        /// Heading of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public ushort Heading
        {
            get
            {
                return m_heading;
            }
            set
            {
                Dirty = true;
                m_heading = value;
            }
        }        

        [ScriptLoadedEvent]
		public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
        {           
        	GameServer.Database.RegisterDataObject(typeof (DbJumpPoint));                
                               
			Console.WriteLine("JumpPoints DB registered!");
        }
    }
}