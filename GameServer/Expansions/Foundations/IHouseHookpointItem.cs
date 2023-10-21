using System;
using Core.Database;
using Core.Database.Tables;

namespace Core.GS.Housing;

public interface IHouseHookpointItem
{
	bool Attach(House house, uint hookpointID, ushort heading);
	bool Attach(House house, DbHouseHookPointItem hookedItem);
	bool Detach(GamePlayer player);
	int Index { get; }
	String TemplateID { get; }
}