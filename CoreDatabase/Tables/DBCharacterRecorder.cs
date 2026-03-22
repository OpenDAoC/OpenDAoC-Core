using DOL.Database;
using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "CharacterRecorder")]
    public class DBCharacterRecorder : DataObject
    {
        [PrimaryKey(AutoIncrement = true)]
        public int ID { get; set; }

        [DataElement(AllowDbNull = false, Index = true)] public string CharacterID { get; set; }
        [DataElement(AllowDbNull = false)] public string Name { get; set; }
        [DataElement(AllowDbNull = false)] public int IconID { get; set; }
        [DataElement(AllowDbNull = false)] public string ActionsJson { get; set; }
    }
}