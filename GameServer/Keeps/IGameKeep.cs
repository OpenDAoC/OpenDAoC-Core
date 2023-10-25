using System.Collections.Generic;
using Core.Database;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.Keeps;

public interface IGameKeep
{
	List<IGameKeepComponent> SentKeepComponents { get; }
	
	Dictionary<string, GameKeepGuard> Guards { get; }
	Dictionary<string, GameKeepBanner> Banners { get; }

	void LoadFromDatabase(DataObject keep);
	void SaveIntoDatabase();
	
	ushort KeepID { get; }
	
	int X { get; }
	int Y { get; }
	int Z { get; }
	ushort Heading { get; }
	Region CurrentRegion { get; }
	
	GuildUtil Guild { get; }
	ERealm Realm { get; }
	byte Level { get; }
	
	byte EffectiveLevel(byte level);		
}