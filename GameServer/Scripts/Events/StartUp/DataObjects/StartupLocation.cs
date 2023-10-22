using Core.Database;

namespace Core.GS.Scripts;

/// <summary>
/// Table Holding Constraints for Startup Locations.
/// </summary>
[DataTable(TableName="StartupLocation")]
public class StartupLocation : DataObject
{
	protected int m_startupLocactionID;
	protected int m_xPos;
	protected int m_yPos;
	protected int m_zPos;
	protected int m_heading;
	protected int m_region;
	protected int m_minVersion;
	protected int m_realmID;
	protected int m_raceID;
	protected int m_classID;
	protected int m_clientRegionID;

	/// <summary>
	/// Primary Key Auto Increment ID.
	/// </summary>
	[PrimaryKey(AutoIncrement=true)]
	public int StartupLoc_ID {
		get {
			return m_startupLocactionID;
		}
		set {
			Dirty = true; m_startupLocactionID = value;
		}
	}

	/// <summary>
	/// Startup X position
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int XPos {
		get {
			return m_xPos;
		}
		set {
			Dirty = true; m_xPos = value;
		}
	}

	/// <summary>
	/// Startup Y position
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int YPos {
		get {
			return m_yPos;
		}
		set {
			Dirty = true; m_yPos = value;
		}
	}

	/// <summary>
	/// Startup Z position
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int ZPos {
		get {
			return m_zPos;
		}
		set {
			Dirty = true; m_zPos = value;
		}
	}

	/// <summary>
	/// Startup Heading
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int Heading {
		get {
			return m_heading;
		}
		set {
			Dirty = true; m_heading = value;
		}
	}

	/// <summary>
	/// Startup Region
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int Region {
		get {
			return m_region;
		}
		set {
			Dirty = true; m_region = value;
		}
	}

	/// <summary>
	/// Minimum Version to allow starting in this location.
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int MinVersion {
		get {
			return m_minVersion;
		}
		set {
			Dirty = true; m_minVersion = value;
		}
	}
	
	/// <summary>
	/// Race ID for this startup Location
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int RealmID {
		get {
			return m_realmID;
		}
		set {
			Dirty = true; m_realmID = value;
		}
	}

	/// <summary>
	/// Race ID for this startup Location
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int RaceID {
		get {
			return m_raceID;
		}
		set {
			Dirty = true; m_raceID = value;
		}
	}
	
	/// <summary>
	/// Class ID for this startup Location.
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int ClassID {
		get {
			return m_classID;
		}
		set {
			Dirty = true; m_classID = value;
		}
	}

	/// <summary>
	/// Client can send Alternate Region ID, 27 for access to tutorial zone, 51 Alb SI, 151 Mid SI, 181 Hib SI 
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int ClientRegionID {
		get {
			return m_clientRegionID;
		}
		set {
			Dirty = true; m_clientRegionID = value;
		}
	}
}