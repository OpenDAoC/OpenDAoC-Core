using System;
using DOL.GS;

namespace DOL.Events
{
	/// <summary>
	/// Holfs the arguments for AreaEvents, this one can be used for either player, npcs or monster Enter/Leave
	/// </summary>
	public class AreaEventArgs : EventArgs
	{
		/// <summary>
		/// Area
		/// </summary>
		IArea m_area;

		/// <summary>
		///  Object either entering or leaving area
		/// </summary>
		GameObject m_object;

		public AreaEventArgs(IArea area, GameObject obj)
		{
			m_area = area;
			m_object = obj;
		}

		/// <summary>
		/// Gets the area
		/// </summary>
		public IArea Area
		{
			get {return m_area;}
		}


		/// <summary>
		/// Gets the gameobject
		/// </summary>
		public GameObject GameObject
		{
			get{return m_object;}
		}
	}
}