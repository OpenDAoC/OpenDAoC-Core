using DOL.Events;
using DOL.Language;

namespace DOL.GS
{	
	/// <summary>
	/// Interface for areas within game, extend this or AbstractArea if you need to define a new area shape that isn't already defined.
	/// Defined ones:
	/// - Area.Cricle
	/// - Area.Square
	/// </summary>
	public interface IArea : ITranslatableObject
	{					
		/// <summary>
		/// Returns the ID of this zone
		/// </summary>
		ushort ID{ get; set;}		

		void UnRegisterPlayerEnter(CoreEventHandler callback);
		void UnRegisterPlayerLeave(CoreEventHandler callback);
		void RegisterPlayerEnter(CoreEventHandler callback);
		void RegisterPlayerLeave(CoreEventHandler callback);

		/// <summary>
		/// Checks wether is intersects with given zone.
		/// This is needed to build an area.zone mapping cache for performance.		
		/// </summary>
		/// <param name="zone"></param>
		/// <returns></returns>
		bool IsIntersectingZone(Zone zone);
		
		/// <summary>
		/// Checks wether given spot is within areas range or not
		/// </summary>
		/// <param name="spot"></param>
		/// <returns></returns>
		bool IsContaining(IPoint3D spot);

		bool IsContaining(IPoint3D spot, bool checkZ);

		bool IsContaining(int x, int y, int z);

		bool IsContaining(int x, int y, int z, bool checkZ);
		
		/// <summary>
		/// Called whenever a player leaves the given area
		/// </summary>
		/// <param name="player"></param>
		void OnPlayerLeave(GamePlayer player);

		/// <summary>
		/// Called whenever a player enters the given area
		/// </summary>
		/// <param name="player"></param>
		void OnPlayerEnter(GamePlayer player);
	}
}
