using System;
using Core.Database;

namespace Core.GS
{
	/// <summary>
	/// Helper class for region registration
	/// </summary>
	public class RegionData : IComparable
	{
		/// <summary>
		/// The region id
		/// </summary>
		public ushort Id;
		/// <summary>
		/// The region name
		/// </summary>
		public string Name;
		/// <summary>
		/// The region description
		/// </summary>
		public string Description;
		/// <summary>
		/// The region IP
		/// </summary>
		public string Ip;
		/// <summary>
		/// The region port
		/// </summary>
		public ushort Port;
		/// <summary>
		/// The region water level
		/// </summary>
		public int WaterLevel;
		/// <summary>
		/// The region diving flag
		/// </summary>
		public bool DivingEnabled;
		/// <summary>
		/// The region housing flag
		/// </summary>
		public bool HousingEnabled;
		/// <summary>
		/// The region expansion
		/// </summary>
		public int Expansion;
		/// <summary>
		/// The region mobs
		/// </summary>
		public DbMob[] Mobs;
		/// <summary>
		/// The class type of this region, blank for default
		/// </summary>
		public string ClassType;
		/// <summary>
		/// Should this region be treated as part of the Frontier?
		/// </summary>
		public bool IsFrontier;

		/// <summary>
		/// Compares 2 objects
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			RegionData cmp = obj as RegionData;
			if (cmp == null) return -1;
			return cmp.Mobs.Length - Mobs.Length;
		}
	}
}
