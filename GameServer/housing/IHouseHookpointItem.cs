using System;
using DOL.Database;

namespace DOL.GS.Housing
{
	/// <summary>
	/// House item interface.
	/// </summary>
	/// <author>Aredhel</author>
	public interface IHouseHookpointItem
	{
		bool Attach(House house, uint hookpointID);
		bool Attach(House house, DbHouseHookPointItem hookedItem);
		bool Detach(GamePlayer player);
		int Index { get; }
		String TemplateID { get; }
	}
}
