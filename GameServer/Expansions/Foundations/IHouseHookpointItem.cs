using System;
using Core.Database.Tables;

namespace Core.GS.Expansions.Foundations;

public interface IHouseHookpointItem
{
	bool Attach(House house, uint hookpointID, ushort heading);
	bool Attach(House house, DbHouseHookPointItem hookedItem);
	bool Detach(GamePlayer player);
	int Index { get; }
	String TemplateID { get; }
}