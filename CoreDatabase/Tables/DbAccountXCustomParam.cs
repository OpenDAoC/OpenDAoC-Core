using DOL.Database.Attributes;

namespace DOL.Database;

/// <summary>
/// Account Custom Params linked to Account Entry
/// </summary>
[DataTable(TableName = "AccountXCustomParam")]
public class DbAccountXCustomParam : CustomParam
{
    private string m_name;
		
    /// <summary>
    /// Account Table Account Name Reference
    /// </summary>
    [DataElement(AllowDbNull = false, Index = true, Varchar = 255)]
    public string Name {
        get { return m_name; }
        set { Dirty = true; m_name = value; }
    }
				
    /// <summary>
    /// Create new instance of <see cref="DbAccountXCustomParam"/> linked to Account Name
    /// </summary>
    /// <param name="Name">Account Name</param>
    /// <param name="KeyName">Key Name</param>
    /// <param name="Value">Value</param>
    public DbAccountXCustomParam(string Name, string KeyName, string Value)
        : base(KeyName, Value)
    {
        this.Name = Name;
    }
		
    /// <summary>
    /// Create new instance of <see cref="DbAccountXCustomParam"/>
    /// </summary>
    public DbAccountXCustomParam()
    {
    }
}