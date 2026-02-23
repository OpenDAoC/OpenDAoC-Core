using DOL.Database;
using DOL.Database.Attributes;
using System;

namespace DOL.Database
{
    [DataTable(TableName = "CharacterRecorder")]
    public class DBCharacterRecorder : DataObject
    {
        // This table stores the recorder macros for each character.
        [PrimaryKey(AutoIncrement = true)]
        public int ID { get; set; }
        private DateTime m_lasttimerowupdated = DateTime.Now;

        [DataElement(AllowDbNull = false)] public string CharacterID { get; set; }
        [DataElement(AllowDbNull = false)] public string Name { get; set; }
        [DataElement(AllowDbNull = false)] public int IconID { get; set; }
        [DataElement(AllowDbNull = false)] public string ActionsJson { get; set; }

        [DataElement(AllowDbNull = false)] 
        public DateTime LastTimeRowUpdated 
        { 
            get { return m_lasttimerowupdated; } 
            set { m_lasttimerowupdated = value; } 
            }
    }
}