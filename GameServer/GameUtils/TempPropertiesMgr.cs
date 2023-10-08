namespace DOL.GS
{
    public abstract class TempPropertiesMgr
    {
        public static ReaderWriterList<TempPropContainer> TempPropContainerList = new ReaderWriterList<TempPropContainer>();

        public class TempPropContainer
        {
            private string m_ownerid;
            public string OwnerID
            {
                get { return m_ownerid; }
            }

            private string m_tempropstring;
            public string TempPropString
            {
                get { return m_tempropstring; }
            }

            private string m_value;
            public string Value
            {
                get { return m_value; }
                set { m_value = value; }
            }
            public TempPropContainer(string ownerid, string tempropstring, string value)
            {
                m_ownerid = ownerid;
                m_tempropstring = tempropstring;
                m_value = value;
            }
        }
    }
}