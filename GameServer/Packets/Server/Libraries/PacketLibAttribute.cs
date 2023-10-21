using System;

namespace DOL.GS.PacketHandler
{
	/// <summary>
	/// Denotes a class as a packet lib.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
	public class PacketLibAttribute : Attribute
	{
		/// <summary>
		/// Stores version Id sent by the client.
		/// </summary>
		int m_rawVersion;
		/// <summary>
		/// PacketLib client version.
		/// </summary>
		GameClient.eClientVersion m_clientVersion;

		/// <summary>
		/// Constructs a new PacketLibAttribute.
		/// </summary>
		/// <param name="rawVersion">The version Id sent by the client.</param>
		/// <param name="clientVersion">PacketLib client version.</param>
		public PacketLibAttribute(int rawVersion, GameClient.eClientVersion clientVersion)
		{
			m_rawVersion = rawVersion;
			m_clientVersion = clientVersion;
		}

		/// <summary>
		/// Gets version Id sent by the client.
		/// </summary>
		public int RawVersion
		{
			get { return m_rawVersion; }
		}

		/// <summary>
		/// Gets the client version for which PacketLib is built.
		/// </summary>
		public GameClient.eClientVersion ClientVersion
		{
			get { return m_clientVersion; }
		}
	}
}
